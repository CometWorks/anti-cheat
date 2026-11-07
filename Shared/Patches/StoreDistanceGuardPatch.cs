// Magnetar defines MAGNETAR when it compiles the plugin from source, DEDICATED is the local build's symbol
#if DEDICATED || MAGNETAR
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Shared.Plugin;
using VRage.Game;
using VRage.Network;
using VRageMath;

namespace Shared.Patches;

// The game checks only block access on Store block requests, so a modified client can trade from anywhere.
// These guards refuse buy, sell and item list requests on every Store block (stores, ATMs, vending machines) unless
// the sender's character is within MyConstants.DEFAULT_INTERACTIVE_DISTANCE, the reach of the game's use ray.
// That ray starts at the player's head, so the gap is measured from the character's whole collision box, not only
// from its feet. A seated character counts where its seat is. The refused client still gets a failed answer, so
// it does not hang.

// The mod API has a public GetStoreItems(List<IMyStoreItem>) too
[HarmonyPatch(typeof(MyStoreBlock), "GetStoreItems", typeof(bool))]
public static class StoreItemsDistanceGuardPatch
{
    private static readonly MethodInfo OnGetStoreItemsResult = AccessTools.Method(
        typeof(MyStoreBlock),
        "OnGetStoreItemsResult"
    );

    private static bool Prefix(MyStoreBlock __instance, bool isPlayerRequest)
    {
        // Every client asks each vending machine for its items once, from wherever the player is, to fill its screen
        if (!isPlayerRequest && __instance is MyVendingMachine)
            return true;

        if (StoreDistanceGuard.IsInRange(__instance, "item list"))
            return true;

        var isNpc =
            MySession.Static.Factions.GetStationByGridId(__instance.CubeGrid.EntityId) != null;
        MyMultiplayer.RaiseEvent(
            __instance,
            x =>
                (Action<List<MyStoreItem>, double, float, float, bool>)
                    OnGetStoreItemsResult.CreateDelegate(
                        typeof(Action<List<MyStoreItem>, double, float, float, bool>),
                        x
                    ),
            new List<MyStoreItem>(),
            0.0,
            1f,
            1f,
            isNpc,
            MyEventContext.Current.Sender
        );
        return false;
    }
}

[HarmonyPatch(typeof(MyStoreBlock), "BuyItem")]
public static class StoreBuyDistanceGuardPatch
{
    private static readonly Action<MyStoreBlock, long, int, MyStoreBuyItemResults> SendResult =
        AccessTools.MethodDelegate<Action<MyStoreBlock, long, int, MyStoreBuyItemResults>>(
            AccessTools.Method(
                typeof(MyStoreBlock),
                "SendBuyItemResult",
                new[] { typeof(long), typeof(int), typeof(MyStoreBuyItemResults) }
            )
        );

    private static bool Prefix(MyStoreBlock __instance, long id, int amount)
    {
        if (StoreDistanceGuard.IsInRange(__instance, "buy"))
            return true;

        SendResult(__instance, id, amount, MyStoreBuyItemResults.ItemsTimeout);
        return false;
    }
}

[HarmonyPatch(typeof(MyStoreBlock), "SellItem")]
public static class StoreSellDistanceGuardPatch
{
    private static readonly Action<MyStoreBlock, long, int, MyStoreSellItemResults> SendResult =
        AccessTools.MethodDelegate<Action<MyStoreBlock, long, int, MyStoreSellItemResults>>(
            AccessTools.Method(
                typeof(MyStoreBlock),
                "SendSellItemResult",
                new[] { typeof(long), typeof(int), typeof(MyStoreSellItemResults) }
            )
        );

    private static bool Prefix(MyStoreBlock __instance, long id, int amount)
    {
        if (StoreDistanceGuard.IsInRange(__instance, "sell"))
            return true;

        SendResult(__instance, id, amount, MyStoreSellItemResults.ItemsTimeout);
        return false;
    }
}

internal static class StoreDistanceGuard
{
    private static readonly double RangeSquared =
        MyConstants.DEFAULT_INTERACTIVE_DISTANCE * MyConstants.DEFAULT_INTERACTIVE_DISTANCE;

    internal static bool IsInRange(MyStoreBlock store, string request)
    {
        if (
            Common.Config?.Enabled != true
            || !Common.Config.EnforceStoreDistance
            || MyEventContext.Current.IsLocallyInvoked
        )
            return true;

        var sender = MyEventContext.Current.Sender.Value;
        var character = MySession.Static.Players.TryGetPlayerBySteamId(sender, out var player)
            ? player.Character
            : null;
        var distanceSquared =
            character == null
                ? double.PositiveInfinity
                : GapSquared(store.PositionComp.WorldAABB, character.PositionComp.WorldAABB);
        if (distanceSquared < RangeSquared)
            return true;

        Common.Logger?.Info(
            $"Refused store {request} from {sender} at {Math.Sqrt(distanceSquared):0.0} m from {store.DisplayNameText} ({store.EntityId})"
        );
        return false;
    }

    private static double GapSquared(BoundingBoxD a, BoundingBoxD b) =>
        Vector3D.Max(Vector3D.Zero, Vector3D.Max(a.Min - b.Max, b.Min - a.Max)).LengthSquared();
}
#endif
