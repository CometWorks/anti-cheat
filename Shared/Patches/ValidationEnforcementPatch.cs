#if DEDICATED
using System;
using System.Collections.Generic;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Shared.Plugin;

namespace Shared.Patches;

[HarmonyPatch(typeof(MyMultiplayerServerBase), nameof(MyMultiplayerServerBase.ValidationFailed))]
public static class ValidationEnforcementPatch
{
    private sealed class FailureWindow
    {
        public int Count;
        public DateTime First;
        public DateTime Last;
    }

    private static readonly Dictionary<ulong, FailureWindow> Failures = new Dictionary<ulong, FailureWindow>();
    private static readonly HashSet<ulong> Kicked = new HashSet<ulong>();
    private static readonly List<ulong> Expired = new List<ulong>();

    [HarmonyPostfix]
    private static void Postfix(ulong clientId, bool kick, string additionalInfo)
    {
        var config = Common.Config;
        if (config == null || !config.Enabled || !config.KickOnValidationFailure || !kick || clientId == 0 || clientId == Sync.ServerId)
            return;

        var now = DateTime.UtcNow;
        var windowSeconds = Math.Max(1, config.ValidationFailureWindowSeconds);
        if (!Failures.TryGetValue(clientId, out var failure) || (now - failure.First).TotalSeconds > windowSeconds)
        {
            failure = new FailureWindow { First = now };
            Failures[clientId] = failure;
        }

        failure.Count++;
        failure.Last = now;

        var threshold = Math.Max(1, config.ValidationFailuresBeforeKick);
        if (failure.Count < threshold || Kicked.Contains(clientId))
            return;

        Kicked.Add(clientId);
        Common.Logger?.Warning(
            "Kicking {0}: {1} validation failures in {2}s. Last: {3}",
            clientId,
            failure.Count,
            windowSeconds,
            additionalInfo ?? "unknown");
        MyMultiplayer.Static?.KickClient(clientId);
    }

    public static void Update()
    {
        if (Common.Plugin == null || Common.Plugin.Tick % 60 != 0)
            return;

        var config = Common.Config;
        if (config == null)
            return;

        var cutoff = DateTime.UtcNow.AddSeconds(-Math.Max(1, config.ValidationFailureWindowSeconds) * 2);
        Expired.Clear();
        foreach (var item in Failures)
        {
            if (item.Value.Last < cutoff)
                Expired.Add(item.Key);
        }

        foreach (var clientId in Expired)
        {
            Failures.Remove(clientId);
            Kicked.Remove(clientId);
        }
    }
}
#endif
