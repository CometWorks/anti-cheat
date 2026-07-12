#if DEDICATED
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Shared.Plugin;
using VRage.Game;
using VRage.Network;

namespace Shared.Patches;

[HarmonyPatch(typeof(MyToolBarCollection))]
public static class ToolbarRequestPatch
{
    private const int MaxToolbarIndex = MyToolbar.DEF_SLOT_COUNT * MyToolbar.DEF_PAGE_COUNT;

    [HarmonyPrefix]
    [HarmonyPatch("OnClearSlotRequest")]
    private static bool OnClearSlotRequestPrefix(int index)
    {
        return IsValidIndex(index, "toolbar clear slot");
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnChangeSlotItemRequest")]
    private static bool OnChangeSlotItemRequestPrefix(int index)
    {
        return IsValidIndex(index, "toolbar change definition slot");
    }

    [HarmonyPrefix]
    [HarmonyPatch("OnChangeSlotBuilderItemRequest")]
    private static bool OnChangeSlotBuilderItemRequestPrefix(int index, MyObjectBuilder_ToolbarItem itemBuilder)
    {
        if (!IsEnabled())
            return true;

        if (itemBuilder == null)
            return Reject("toolbar change builder slot with null item");

        return IsValidIndex(index, "toolbar change builder slot");
    }

    private static bool IsValidIndex(int index, string reason)
    {
        if (!IsEnabled())
            return true;

        if (index >= 0 && index < MaxToolbarIndex)
            return true;

        return Reject($"{reason} index {index}");
    }

    private static bool IsEnabled()
    {
        return Common.Config != null && Common.Config.Enabled && Common.Config.RejectInvalidToolbarRequests;
    }

    private static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}
#endif
