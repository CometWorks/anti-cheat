#if DEDICATED
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.World;
using Shared.Plugin;
using VRage.Game;
using VRage.Game.Entity;
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
}
#endif
