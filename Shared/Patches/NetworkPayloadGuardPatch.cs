#if DEDICATED
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Shared.Plugin;
using VRage.Network;
using VRageMath;

namespace Shared.Patches;

[HarmonyPatch(typeof(MyMultiplayerBase), "OnBlockListReceived_Server")]
public static class BlockListGuardPatch
{
    private static bool Prefix(BlockList blockList)
    {
        if (!IsEnabled())
            return true;

        var count = blockList.BlockedUsers?.Length ?? 0;
        if (count <= Math.Max(0, Common.Config.MaxSocialListEntries))
            return true;

        return Reject($"block list too large: {count}");
    }

    private static bool IsEnabled()
    {
        return Common.Config != null && Common.Config.Enabled && Common.Config.RejectOversizedSocialLists && Common.Config.MaxSocialListEntries >= 0;
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}

[HarmonyPatch(typeof(MyMultiplayerBase), nameof(MyMultiplayerBase.MutedPlayersUpdate))]
public static class MutedPlayersGuardPatch
{
    private static bool Prefix(HashSet<ulong> mutedPlayers)
    {
        if (Common.Config == null || !Common.Config.Enabled || !Common.Config.RejectOversizedSocialLists || Common.Config.MaxSocialListEntries < 0)
            return true;

        var count = mutedPlayers?.Count ?? 0;
        if (count <= Common.Config.MaxSocialListEntries)
            return true;

        return Reject($"muted player list too large: {count}");
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}

[HarmonyPatch(typeof(MyClientState), nameof(MyClientState.AddKnownSector))]
public static class KnownSectorGuardPatch
{
    private static bool Prefix(long planetId, long sectorId)
    {
        if (Common.Config == null || !Common.Config.Enabled || !Common.Config.RejectKnownSectorOverflow || Common.Config.MaxKnownSectorsPerClient < 0)
            return true;

        var server = MyMultiplayer.Static?.ReplicationLayer as MyReplicationServer;
        var state = server?.GetClientData(new Endpoint(MyEventContext.Current.Sender, 0)) as MyClientState;
        if (state == null)
            return true;

        if (state.KnownSectors.TryGetValue(planetId, out var sectors) && sectors.Contains(sectorId))
            return true;

        var count = 0;
        foreach (var item in state.KnownSectors)
            count += item.Value.Count;

        if (count < Common.Config.MaxKnownSectorsPerClient)
            return true;

        return Reject($"known sector list too large: {count}");
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}

[HarmonyPatch]
public static class CameraSettingsGuardPatch
{
    private const double MaxCameraDistance = 10000.0;
    private const float MaxHeadAngle = 1000f;

    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Sandbox.Game.Multiplayer.MyCameraCollection"), "OnSaveEntityCameraSettings");
    }

    private static bool Prefix(double distance, Vector2 headAngle)
    {
        if (Common.Config == null || !Common.Config.Enabled || !Common.Config.RejectInvalidCameraSettings)
            return true;

        if (IsFinite(distance) && distance >= 0.0 && distance <= MaxCameraDistance &&
            IsFinite(headAngle.X) && IsFinite(headAngle.Y) &&
            Math.Abs(headAngle.X) <= MaxHeadAngle && Math.Abs(headAngle.Y) <= MaxHeadAngle)
            return true;

        return Reject("invalid camera settings");
    }

    private static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}

[HarmonyPatch]
public static class GpsAddGuardPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Sandbox.Game.Multiplayer.MyGpsCollection"), "AddGpsOnServer");
    }

    private static bool Prefix(object[] __args)
    {
        return GpsStringGuard.IsValid(__args?[0], includeDisplayName: true, "add GPS");
    }
}

[HarmonyPatch]
public static class GpsModifyGuardPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Sandbox.Game.Multiplayer.MyGpsCollection"), "ModifyGpsOnServer");
    }

    private static bool Prefix(object[] __args)
    {
        return GpsStringGuard.IsValid(__args?[0], includeDisplayName: false, "modify GPS");
    }
}

[HarmonyPatch]
public static class FactionCreateGuardPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Sandbox.Game.Multiplayer.MyFactionCollection"), "CreateFactionRequest");
    }

    private static bool Prefix(object[] __args)
    {
        return FactionStringGuard.IsValid(__args?[0], "create faction");
    }
}

[HarmonyPatch]
public static class FactionEditGuardPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(AccessTools.TypeByName("Sandbox.Game.Multiplayer.MyFactionCollection"), "EditFactionRequest");
    }

    private static bool Prefix(object[] __args)
    {
        return FactionStringGuard.IsValid(__args?[0], "edit faction");
    }
}

internal static class GpsStringGuard
{
    public static bool IsValid(object msg, bool includeDisplayName, string reason)
    {
        if (Common.Config == null || !Common.Config.Enabled || !Common.Config.RejectOversizedGpsMessages || Common.Config.MaxGpsStringLength < 0 || msg == null)
            return true;

        var max = Common.Config.MaxGpsStringLength;
        var length = GetLength(msg, "Name") + GetLength(msg, "Description");
        if (includeDisplayName)
            length += GetLength(msg, "DisplayName");

        if (length <= max)
            return true;

        return Reject($"{reason} strings too large: {length}");
    }

    private static int GetLength(object msg, string fieldName)
    {
        return (AccessTools.Field(msg.GetType(), fieldName)?.GetValue(msg) as string)?.Length ?? 0;
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}

internal static class FactionStringGuard
{
    public static bool IsValid(object msg, string reason)
    {
        if (Common.Config == null || !Common.Config.Enabled || !Common.Config.RejectOversizedFactionMessages || Common.Config.MaxFactionStringLength < 0 || msg == null)
            return true;

        var max = Common.Config.MaxFactionStringLength;
        var length =
            GetLength(msg, "FactionTag") +
            GetLength(msg, "FactionName") +
            GetLength(msg, "FactionDescription") +
            GetLength(msg, "FactionPrivateInfo") +
            GetLength(msg, "Type");

        if (length <= max)
            return true;

        return Reject($"{reason} strings too large: {length}");
    }

    private static int GetLength(object msg, string fieldName)
    {
        return (AccessTools.Field(msg.GetType(), fieldName)?.GetValue(msg) as string)?.Length ?? 0;
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}
#endif
