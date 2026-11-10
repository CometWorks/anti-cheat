#if DEDICATED || MAGNETAR
using System.Reflection;
using HarmonyLib;
using Sandbox.Common.ObjectBuilders;
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
/// client who is not the owner or a same-faction member of the grid.
///
/// The patch is a no-op during world saves (no replication endpoint is active).
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

        // Custom Data — frequently contains script-specific data.
        pb.ComponentContainer?.Components?.RemoveAll(c => c.TypeId == "MyModStorageComponent");
    }
}

/// <summary>
/// Postfix patch on the private <c>MyReplicationServer.ShouldSendEvent</c>.
/// This method is the server-side gate that decides whether a given broadcast
/// event should be forwarded to each individual connected client.
///
/// When the event being dispatched is <c>MyProgrammableBlock.UpdateProgram</c>
/// (the [Server][Broadcast] RPC that carries the new script bytes), we
/// additionally require that the recipient is the owner or a same-faction member.
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

        if (Common.Config?.Enabled != true || !Common.Config.HideProgrammableBlockScripts)
            return;

        // Only intercept the UpdateProgram broadcast.
        if (site.MethodInfo != UpdateProgramMethod)
            return;

        // eventInstance is the replicable (MyTerminalReplicable), not the block itself.
        // GetUniqueId() returns Block.EntityId, which we use to look up the PB.
        if (!MyEntities.TryGetEntityById(eventInstance.GetUniqueId(), out MyProgrammableBlock pb))
            return;

        var clientState = ClientStateField?.GetValue(client) as MyClientStateBase;
        if (clientState == null)
            return;

        var steamId = clientState.EndpointId.Id.Value;
        if (ScriptAccessHelper.IsAuthorized(pb, steamId))
            return;

        Common.Logger?.Info(
            "Suppressing UpdateProgram broadcast to client {0} for block {1}",
            steamId,
            pb.EntityId);

        __result = false;
    }
}

/// <summary>
/// Postfix patch on the private <c>MyReplicationServer.ShouldSendEvent</c>.
/// When the event being dispatched is <c>MyProgrammableBlock.SubscribeToProgramResponse</c>
/// (the [Server] RPC that handles subscription to program output), we
/// additionally require that the caller is the owner or a same-faction member.
/// Unauthorized clients receive <c>false</c>, so the RPC is never processed.
/// </summary>
[HarmonyPatch]
public static class ProgrammableBlockSubscriptionFilterPatch
{
    // The network RPC handler: private void SubscribeToProgramResponse(bool register)
    private static readonly MethodInfo SubscribeToProgramResponseMethod =
        AccessTools.Method(typeof(MyProgrammableBlock), "SubscribeToProgramResponse", [typeof(bool)]);

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

        if (Common.Config?.Enabled != true || !Common.Config.HideProgrammableBlockScripts)
            return;

        // Only intercept the SubscribeToProgramResponse RPC.
        if (site.MethodInfo != SubscribeToProgramResponseMethod)
            return;

        // eventInstance is the replicable (MyTerminalReplicable), not the block itself.
        // GetUniqueId() returns Block.EntityId, which we use to look up the PB.
        if (!MyEntities.TryGetEntityById(eventInstance.GetUniqueId(), out MyProgrammableBlock pb))
            return;

        var clientState = ClientStateField?.GetValue(client) as MyClientStateBase;
        if (clientState == null)
            return;

        var steamId = clientState.EndpointId.Id.Value;
        if (ScriptAccessHelper.IsAuthorized(pb, steamId))
            return;

        Common.Logger?.Debug(
            "Blocking SubscribeToProgramResponse RPC for unauthorized client {0} on block {1}",
            steamId,
            pb.EntityId);

        __result = false;
    }
}
#endif
