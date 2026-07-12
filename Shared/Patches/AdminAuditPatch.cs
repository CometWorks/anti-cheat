#if DEDICATED
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.SessionComponents.Clipboard;
using Sandbox.Game.World;
using Shared.Plugin;
using VRage.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRage.Utils;
using VRageMath;

namespace Shared.Patches;

[HarmonyPatch]
public static class CreativeToolsAuditPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(MySession), "OnCreativeToolsEnabled");
    }

    private static void Postfix(bool value)
    {
        if (!AdminAudit.Enabled || MySession.Static == null)
            return;

        var steamId = VRage.Network.MyEventContext.Current.Sender.Value;
        AdminAudit.Log("creative-tools {0}: player={1}", value ? "enabled" : "disabled", AdminAudit.Player(steamId));
    }
}

[HarmonyPatch]
public static class CreativeBuildAuditPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(
            typeof(MyCubeGrid),
            "BuildBlock",
            new[]
            {
                typeof(MyCubeBlockDefinition),
                typeof(Vector3),
                typeof(MyStringHash),
                typeof(Vector3I),
                typeof(Quaternion),
                typeof(long),
                typeof(long),
                typeof(MyEntity),
                typeof(MyObjectBuilder_CubeBlock),
                typeof(bool),
                typeof(bool),
                typeof(bool),
                typeof(string)
            });
    }

    private static void Postfix(MySlimBlock __result, MyCubeGrid __instance, MyCubeBlockDefinition blockDefinition, Vector3I min, long owner, bool buildAsAdmin)
    {
        if (__result == null || !AdminAudit.Enabled || MySession.Static == null)
            return;

        var steamId = MySession.Static.Players.TryGetSteamId(owner);
        var creativeTools = steamId != 0 && MySession.Static.CreativeToolsEnabled(steamId);
        var creativeMode = MySession.Static.CreativeMode;
        if (!buildAsAdmin && !creativeTools && !creativeMode)
            return;

        AdminAudit.Log(
            "creative-build: player={0} block={1} grid={2} pos={3} admin={4} creativeTools={5} creativeWorld={6}",
            AdminAudit.Player(steamId, owner),
            blockDefinition?.Id.ToString() ?? "unknown",
            __instance.EntityId,
            min,
            buildAsAdmin,
            creativeTools,
            creativeMode);
    }
}

[HarmonyPatch(typeof(MySlimBlock), nameof(MySlimBlock.IncreaseMountLevel))]
public static class CreativeWeldAuditPatch
{
    private static void Postfix(bool __result, MySlimBlock __instance, long welderOwnerIdentId, bool handWelded)
    {
        if (!__result || !AdminAudit.Enabled || MySession.Static == null)
            return;

        var steamId = MySession.Static.Players.TryGetSteamId(welderOwnerIdentId);
        var creativeTools = steamId != 0 && MySession.Static.CreativeToolsEnabled(steamId);
        var creativeMode = MySession.Static.CreativeMode;
        if (!creativeTools && !creativeMode)
            return;

        var gridId = __instance.CubeGrid?.EntityId ?? 0;
        var key = $"weld:{welderOwnerIdentId}:{gridId}:{__instance.Position}";
        if (!AdminAudit.ShouldLog(key))
            return;

        AdminAudit.Log(
            "creative-weld: player={0} block={1} grid={2} pos={3} hand={4} creativeTools={5} creativeWorld={6}",
            AdminAudit.Player(steamId, welderOwnerIdentId),
            __instance.BlockDefinition?.Id.ToString() ?? "unknown",
            gridId,
            __instance.Position,
            handWelded,
            creativeTools,
            creativeMode);
    }
}

internal static class AdminAudit
{
    private static readonly Dictionary<string, DateTime> LastLog = new Dictionary<string, DateTime>();

    public static bool Enabled => Common.Config != null && Common.Config.Enabled && Common.Config.AdminAuditEnabled;
    public static bool ExtendedEnabled => Enabled && Common.Config.AdminAuditExtendedEnabled;

    public static bool ShouldLog(string key)
    {
        var seconds = Math.Max(0, Common.Config?.AdminAuditLogIntervalSeconds ?? 10);
        if (seconds == 0)
            return true;

        var now = DateTime.UtcNow;
        if (LastLog.TryGetValue(key, out var last) && (now - last).TotalSeconds < seconds)
            return false;

        LastLog[key] = now;
        return true;
    }

    public static string Player(ulong steamId, long identityId = 0)
    {
        if (MySession.Static == null)
            return $"{identityId}/{steamId}";

        if (identityId == 0 && steamId != 0)
            identityId = MySession.Static.Players.TryGetIdentityId(steamId);

        var identity = MySession.Static.Players.TryGetIdentity(identityId);
        var name = identity?.DisplayName ?? "unknown";
        return $"{name}/{identityId}/{steamId}";
    }

    public static void Log(string message, params object[] args)
    {
        Common.Logger?.Info("ADMIN_AUDIT " + message, args);
    }

    public static bool IsElevated(ulong steamId)
    {
        if (steamId == 0 || MySession.Static == null)
            return false;

        return MySession.Static.IsUserAdmin(steamId)
               || MySession.Static.GetUserPromoteLevel(steamId) >= MyPromoteLevel.SpaceMaster
               || MySession.Static.CreativeToolsEnabled(steamId)
               || (MySession.Static.RemoteAdminSettings.TryGetValue(steamId, out var settings) && settings != AdminSettingsEnum.None);
    }
}

[HarmonyPatch(typeof(MyCubeBlock), nameof(MyCubeBlock.ChangeOwner))]
public static class OwnershipAuditPatch
{
    private static void Prefix(MyCubeBlock __instance, out long __state)
    {
        __state = __instance.OwnerId;
    }

    private static void Postfix(MyCubeBlock __instance, long owner, MyOwnershipShareModeEnum shareMode, long __state)
    {
        if (!AdminAudit.ExtendedEnabled || __state == owner)
            return;

        var steamId = MyEventContext.Current.Sender.Value;
        AdminAudit.Log(
            "ownership-change: player={0} block={1}/{2} grid={3} oldOwner={4} newOwner={5} share={6}",
            AdminAudit.Player(steamId),
            __instance.BlockDefinition?.Id.ToString() ?? "unknown",
            __instance.EntityId,
            __instance.CubeGrid?.EntityId ?? 0,
            __state,
            owner,
            shareMode);
    }
}

[HarmonyPatch(typeof(MyCubeGrid), nameof(MyCubeGrid.TryPasteGrid_Implementation))]
public static class AdminPasteAuditPatch
{
    private static void Prefix(MyCubeGrid.MyPasteGridParameters parameters)
    {
        if (!AdminAudit.ExtendedEnabled)
            return;

        var steamId = MyEventContext.Current.Sender.Value;
        if (!AdminAudit.IsElevated(steamId))
            return;

        var grids = parameters.Entities?.Count ?? 0;
        var blocks = 0;
        if (parameters.Entities != null)
        {
            foreach (var grid in parameters.Entities)
                blocks += grid?.CubeBlocks?.Count ?? 0;
        }

        AdminAudit.Log(
            "admin-paste: player={0} grids={1} blocks={2} instant={3} velocity={4}",
            AdminAudit.Player(steamId),
            grids,
            blocks,
            parameters.InstantBuild,
            parameters.ObjectVelocity);
    }
}

[HarmonyPatch]
public static class AdminCutAuditPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(MyClipboardComponent), "OnCutConfirm");
    }

    private static void Prefix(MyCubeGrid targetGrid, bool cutGroup, bool cutOverLgs)
    {
        Log("admin-cut", targetGrid, cutGroup, cutOverLgs);
    }

    internal static void Log(string action, MyCubeGrid targetGrid, bool cutGroup, bool cutOverLgs)
    {
        if (!AdminAudit.ExtendedEnabled)
            return;

        var steamId = MyEventContext.Current.Sender.Value;
        if (!AdminAudit.IsElevated(steamId))
            return;

        AdminAudit.Log(
            "{0}: player={1} grid={2} blocks={3} group={4} physicalLinks={5}",
            action,
            AdminAudit.Player(steamId),
            targetGrid?.EntityId ?? 0,
            targetGrid?.BlocksCount ?? 0,
            cutGroup,
            cutOverLgs);
    }
}

[HarmonyPatch]
public static class AdminDeleteAuditPatch
{
    private static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(MyClipboardComponent), "OnDeleteConfirm");
    }

    private static void Prefix(MyCubeGrid targetGrid, bool cutGroup, bool cutOverLgs)
    {
        AdminCutAuditPatch.Log("admin-delete", targetGrid, cutGroup, cutOverLgs);
    }
}
#endif
