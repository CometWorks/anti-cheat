#if DEDICATED || MAGNETAR
using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Shared.Plugin;
using VRage.Game;
using VRage.Network;

namespace Shared.Patches;

/// <summary>
/// Postfix patch on <see cref="MyProgrammableBlock.GetObjectBuilderCubeBlock"/>.
/// When the game serializes a grid for replication to a specific client,
/// this patch blanks the <c>Program</c> field in the object builder for any
/// client who is not allowed to edit the block.
///
/// The patch is a no-op during world saves (no replication endpoint is active).
/// Projector blueprints are not covered yet and still carry PB programs.
/// </summary>
[HarmonyPatch(typeof(MyProgrammableBlock), nameof(MyProgrammableBlock.GetObjectBuilderCubeBlock))]
public static class ProgrammableBlockObjectBuilderFilterPatch
{
    private static void Postfix(MyProgrammableBlock __instance, ref MyObjectBuilder_CubeBlock __result)
    {
        if (Common.Config?.Enabled != true || !Common.Config.HideProgrammableBlockScripts)
            return;

        if (__result is not MyObjectBuilder_MyProgrammableBlock pb)
            return;

        // No program to hide.
        if (string.IsNullOrEmpty(pb.Program))
            return;

        // Only filter during grid streaming to a specific client.
        var steamId = MyReplicationLayer.CurrentSerializationDestinationEndpoint.Id.Value;
        if (steamId == 0 || ScriptAccessHelper.IsAuthorized(__instance, steamId))
            return;

        Common.Logger?.Debug(
            "Hiding PB script from client {0} on block {1} (owner {2})",
            steamId,
            __instance.EntityId,
            __instance.OwnerId);

        RedactProgramData(pb);
    }

    /// <summary>
    /// Blanks all script-related fields from a PB object builder.
    /// Extracted for testability — has no dependency on game or plugin statics.
    /// </summary>
    internal static void RedactProgramData(MyObjectBuilder_MyProgrammableBlock pb)
    {
        pb.Program = string.Empty;
        pb.Storage = string.Empty;
        pb.DefaultRunArgument = string.Empty;
    }
}

/// <summary>
/// Postfix patch on the private <c>MyReplicationServer.ShouldSendEvent</c>.
/// This method is the server-side gate that decides whether a given broadcast
/// event should be forwarded to each individual connected client.
///
/// When the event being dispatched is <c>MyProgrammableBlock.UpdateProgram</c>
/// (the [Server][Broadcast] RPC that carries the new script bytes), we
/// additionally require that the recipient is allowed to edit the block.
/// Unauthorized clients receive <c>false</c>, so the packet is never sent to them.
/// </summary>
[HarmonyPatch]
public static class ProgrammableBlockBroadcastFilterPatch
{
    // The network RPC handler: private void UpdateProgram(byte[] program)
    private static readonly MethodInfo UpdateProgramMethod =
        AccessTools.Method(typeof(MyProgrammableBlock), "UpdateProgram", [typeof(byte[])]);

    // MyClient is internal; access its public State field via reflection.
    private static readonly FieldInfo ClientStateField =
        AccessTools.Field(AccessTools.TypeByName("VRage.Network.MyClient"), "State");

    // Harmony requires a TargetMethod when [HarmonyPatch] has no type/method arguments.
    private static MethodBase TargetMethod() =>
        AccessTools.Method(typeof(MyReplicationServer), "ShouldSendEvent");

    private static void Postfix(
        IMyNetObject eventInstance,
        CallSite site,
        object client,
        ref bool __result)
    {
        // Skip if the base method already decided not to send.
        if (!__result)
            return;

        // Only intercept the UpdateProgram broadcast.
        if (site.MethodInfo != UpdateProgramMethod)
            return;

        var clientState = ClientStateField?.GetValue(client) as MyClientStateBase;
        if (clientState == null)
            return;

        var steamId = clientState.EndpointId.Id.Value;

        // The synthetic program push sent before OpenEditorSuccess is only for its requester.
        if (ScriptPushScope.TargetSteamId != 0 && steamId != ScriptPushScope.TargetSteamId)
        {
            __result = false;
            return;
        }

        if (Common.Config?.Enabled != true || !Common.Config.HideProgrammableBlockScripts)
            return;

        // eventInstance is the replicable (MyTerminalReplicable), not the block itself.
        // GetUniqueId() returns Block.EntityId, which we use to look up the PB.
        if (!MyEntities.TryGetEntityById(eventInstance.GetUniqueId(), out MyProgrammableBlock pb))
            return;

        if (ScriptAccessHelper.IsAuthorized(pb, steamId))
            return;

        Common.Logger?.Debug(
            "Suppressing UpdateProgram broadcast to client {0} for block {1}",
            steamId,
            pb.EntityId);

        __result = false;
    }
}

/// <summary>
/// Prefix patch on the private <c>MyProgrammableBlock.OpenEditorRequest</c>.
/// A client that received a redacted block has an empty local program. Before
/// the server lets it open the editor, push the current program to that client
/// so pressing OK does not overwrite the real script with an empty one.
/// </summary>
[HarmonyPatch(typeof(MyProgrammableBlock), "OpenEditorRequest")]
public static class ProgrammableBlockOpenEditorFilterPatch
{
    private static readonly FieldInfo ConsoleOpenField =
        AccessTools.Field(typeof(MyProgrammableBlock), "m_consoleOpen");

    private static readonly FieldInfo ProgramDataField =
        AccessTools.Field(typeof(MyProgrammableBlock), "m_programData");

    private static readonly MethodInfo UpdateProgramMethod =
        AccessTools.Method(typeof(MyProgrammableBlock), "UpdateProgram", [typeof(byte[])]);

    private static readonly Func<MyProgrammableBlock, Action<byte[]>> UpdateProgramAction =
        instance => AccessTools.MethodDelegate<Action<byte[]>>(UpdateProgramMethod, instance, false, null);

    // Same format as the game's internal StringCompressor.CompressString.
    private static byte[] CompressString(string value)
    {
        using (var source = new MemoryStream(Encoding.UTF8.GetBytes(value)))
        using (var target = new MemoryStream())
        {
            using (var gzip = new GZipStream(target, CompressionMode.Compress))
                source.CopyTo(gzip);

            return target.ToArray();
        }
    }

    private static void Prefix(MyProgrammableBlock __instance)
    {
        if (Common.Config?.Enabled != true || !Common.Config.HideProgrammableBlockScripts)
            return;

        // The original method will refuse the request when another editor is already open.
        if (ConsoleOpenField?.GetValue(__instance) is true)
            return;

        var sender = MyEventContext.Current.Sender;
        if (sender.Value == 0 || !ScriptAccessHelper.IsAuthorized(__instance, sender.Value))
            return;

        var program = ProgramDataField?.GetValue(__instance) as string;
        if (string.IsNullOrEmpty(program))
            return;

        // UpdateProgram is a [Server][Broadcast] event. Restrict this push to the
        // requester and skip the local server apply so the script is not recompiled.
        ScriptPushScope.TargetSteamId = sender.Value;
        ScriptPushScope.SuppressLocalApply = true;
        try
        {
            MyMultiplayer.RaiseEvent(__instance, UpdateProgramAction, CompressString(program));
        }
        finally
        {
            ScriptPushScope.TargetSteamId = 0;
            ScriptPushScope.SuppressLocalApply = false;
        }
    }
}

/// <summary>
/// Skips the local server apply of the synthetic <c>UpdateProgram</c> event raised
/// by <see cref="ProgrammableBlockOpenEditorFilterPatch"/>. Without this the server
/// would recompile the unchanged script and restart its runtime whenever an editor opens.
/// </summary>
[HarmonyPatch(typeof(MyProgrammableBlock), "UpdateProgram", typeof(byte[]))]
public static class ProgrammableBlockSyntheticProgramPushPatch
{
    private static bool Prefix() => !ScriptPushScope.SuppressLocalApply;
}

internal static class ScriptPushScope
{
    [ThreadStatic]
    internal static ulong TargetSteamId;

    [ThreadStatic]
    internal static bool SuppressLocalApply;
}
#endif
