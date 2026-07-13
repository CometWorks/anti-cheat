#if DEDICATED
using System;
using System.Collections.Generic;
using HarmonyLib;
using Sandbox.Engine.Multiplayer;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.Entities.Character;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Shared.Plugin;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Network;
using VRageMath;

namespace Shared.Patches;

[HarmonyPatch(typeof(MyInventory), "PickupItem_Implementation")]
public static class FloatingObjectPickupGuardPatch
{
    private static bool Prefix(MyInventory __instance, long entityId, MyFixedPoint amount)
    {
        if (!AuthorityGuard.InventoryEnabled || MyEventContext.Current.IsLocallyInvoked)
            return true;

        var character = AuthorityGuard.SenderCharacter(out _);
        if (amount > MyFixedPoint.Zero && character != null && __instance.Owner == character &&
            MyEntities.TryGetEntityById(entityId, out MyFloatingObject item, false) &&
            AuthorityGuard.IsNear(character, item))
            return true;

        return AuthorityGuard.Reject("invalid floating-item pickup request");
    }
}

[HarmonyPatch(typeof(MyInventory), "InventoryTransferItemPlanner_Implementation")]
public static class InventoryPlannerTransferGuardPatch
{
    private static bool Prefix(MyInventory __instance, long destinationOwnerId, int destInventoryIndex, MyFixedPoint? amount)
    {
        return (!amount.HasValue || amount.Value >= MyFixedPoint.Zero) &&
               AuthorityGuard.IsInventoryTransferAllowed(__instance, destinationOwnerId, destInventoryIndex) ||
               AuthorityGuard.Reject("invalid inventory planner transfer request");
    }
}

[HarmonyPatch(typeof(MyInventory), "InventoryTransferItem_Implementation")]
public static class InventoryTransferGuardPatch
{
    private static bool Prefix(MyInventory __instance, MyFixedPoint amount, long destinationOwnerId, byte destInventoryIndex)
    {
        return amount >= MyFixedPoint.Zero && AuthorityGuard.IsInventoryTransferAllowed(__instance, destinationOwnerId, destInventoryIndex) ||
               AuthorityGuard.Reject("invalid inventory transfer request");
    }
}

[HarmonyPatch(typeof(MyCubeGrid), "BuildBlocksRequest")]
public static class BuildBlocksAuthorityGuardPatch
{
    private static bool Prefix(MyCubeGrid __instance, HashSet<MyCubeGrid.MyBlockLocation> locations, long builderEntityId, long ownerId)
    {
        if (!AuthorityGuard.BuildEnabled || AuthorityGuard.BypassBuildGuard)
            return true;

        var character = AuthorityGuard.ValidateBuilder(builderEntityId, ownerId);
        if (character == null || locations == null || locations.Count == 0 || locations.Count > AuthorityGuard.MaxBuildBatch)
            return AuthorityGuard.Reject("invalid block-build request authority or batch size");

        foreach (var location in locations)
        {
            if (location.Owner != ownerId || !AuthorityGuard.IsNear(character, __instance.GridIntegerToWorld(location.CenterPos)))
                return AuthorityGuard.Reject("spoofed or remote block-build request");
        }

        return true;
    }
}

[HarmonyPatch(typeof(MyCubeGrid), "BuildBlocksAreaRequest")]
public static class BuildAreaAuthorityGuardPatch
{
    private static bool Prefix(MyCubeGrid __instance, ref MyCubeGrid.MyBlockBuildArea area, long builderEntityId, long ownerId, ulong placingPlayer)
    {
        if (!AuthorityGuard.BuildEnabled || AuthorityGuard.BypassBuildGuard)
            return true;

        var character = AuthorityGuard.ValidateBuilder(builderEntityId, ownerId);
        var count = area.BuildAreaSize.X * area.BuildAreaSize.Y * area.BuildAreaSize.Z;
        if (character == null || placingPlayer != MyEventContext.Current.Sender.Value || count <= 0 || count > AuthorityGuard.MaxBuildBatch)
            return AuthorityGuard.Reject("invalid area-build request authority or batch size");

        var last = area.PosInGrid + new Vector3I(
            (area.BuildAreaSize.X - 1) * area.StepDelta.X,
            (area.BuildAreaSize.Y - 1) * area.StepDelta.Y,
            (area.BuildAreaSize.Z - 1) * area.StepDelta.Z);

        return AuthorityGuard.IsNear(character, __instance.GridIntegerToWorld(area.PosInGrid)) &&
               AuthorityGuard.IsNear(character, __instance.GridIntegerToWorld(last)) ||
               AuthorityGuard.Reject("remote area-build request");
    }
}

[HarmonyPatch(typeof(MyProjectorBase), "BuildInternal")]
public static class ProjectorBuildAuthorityGuardPatch
{
    private static bool Prefix(MyProjectorBase __instance, Vector3I cubeBlockPosition, long owner, long builder, long builtBy)
    {
        if (!AuthorityGuard.BuildEnabled || AuthorityGuard.BypassBuildGuard)
            return true;

        var character = AuthorityGuard.ValidateBuilder(builder, owner);
        var projectedBlock = __instance.ProjectedGrid?.GetCubeBlock(cubeBlockPosition);
        if (character == null || builtBy != owner || projectedBlock == null ||
            !AuthorityGuard.IsNear(character, projectedBlock.CubeGrid.GridIntegerToWorld(projectedBlock.Position)))
            return AuthorityGuard.Reject("spoofed or remote projector-build request");

        var relation = __instance.GetUserRelationToOwner(owner);
        if (relation != MyRelationsBetweenPlayerAndBlock.Owner &&
            relation != MyRelationsBetweenPlayerAndBlock.FactionShare &&
            relation != MyRelationsBetweenPlayerAndBlock.NoOwnership)
            return AuthorityGuard.Reject("projector-build request without access");

        return AuthorityGuard.AllowProjectorBuild() || AuthorityGuard.Reject("projector-build request rate exceeded");
    }
}

internal static class AuthorityGuard
{
    internal const int MaxBuildBatch = 2048;
    private const int MaxProjectorBuildsPerSecond = 12;
    private static readonly double MaxReachSquared = Math.Pow(MyConstants.DEFAULT_INTERACTIVE_DISTANCE * 3f, 2);
    private static readonly Dictionary<ulong, BuildWindow> ProjectorBuilds = new Dictionary<ulong, BuildWindow>();

    private sealed class BuildWindow
    {
        public DateTime Started;
        public int Count;
    }

    internal static bool InventoryEnabled => Common.Config?.Enabled == true && Common.Config.EnforceInventoryRequestAuthority;
    internal static bool BuildEnabled => Common.Config?.Enabled == true && Common.Config.EnforceBuildRequestAuthority;

    internal static bool BypassBuildGuard => MyEventContext.Current.IsLocallyInvoked || MySession.Static.CreativeMode ||
                                             MySession.Static.HasPlayerCreativeRights(MyEventContext.Current.Sender.Value);

    internal static MyCharacter SenderCharacter(out long identityId)
    {
        identityId = MySession.Static?.Players.TryGetIdentityId(MyEventContext.Current.Sender.Value) ?? 0L;
        return MySession.Static?.Players.TryGetIdentity(identityId)?.Character;
    }

    internal static MyCharacter ValidateBuilder(long builderEntityId, long ownerId)
    {
        var character = SenderCharacter(out var identityId);
        return character != null && identityId == ownerId && character.EntityId == builderEntityId ? character : null;
    }

    internal static bool IsNear(MyEntity character, MyEntity target)
    {
        return target != null && target.PositionComp.WorldAABB.DistanceSquared(character.PositionComp.GetPosition()) <= MaxReachSquared;
    }

    internal static bool IsNear(MyEntity character, Vector3D target)
    {
        return Vector3D.DistanceSquared(character.PositionComp.GetPosition(), target) <= MaxReachSquared;
    }

    internal static bool IsInventoryTransferAllowed(MyInventory source, long destinationOwnerId, int destinationInventoryIndex)
    {
        if (!InventoryEnabled || MyEventContext.Current.IsLocallyInvoked)
            return true;

        if (source?.Owner == null || !MyEntities.TryGetEntityById(destinationOwnerId, out MyEntity destination, false) ||
            destinationInventoryIndex < 0 || destinationInventoryIndex >= destination.InventoryCount)
            return false;

        if (source.Owner == destination)
            return true;

        if (source.Owner is MyCubeBlock sourceBlock && destination is MyCubeBlock destinationBlock &&
            sourceBlock.CubeGrid?.IsInSameLogicalGroupAs(destinationBlock.CubeGrid) == true)
            return true;

        var character = SenderCharacter(out _);
        return character != null && IsNear(character, source.Owner) && IsNear(character, destination);
    }

    internal static bool AllowProjectorBuild()
    {
        var sender = MyEventContext.Current.Sender.Value;
        var now = DateTime.UtcNow;
        if (!ProjectorBuilds.TryGetValue(sender, out var window) || (now - window.Started).TotalSeconds >= 1)
        {
            ProjectorBuilds[sender] = new BuildWindow { Started = now, Count = 1 };
            return true;
        }

        return ++window.Count <= MaxProjectorBuildsPerSecond;
    }

    internal static bool Reject(string reason)
    {
        if (!MyEventContext.Current.IsLocallyInvoked)
            (MyMultiplayer.Static as MyMultiplayerServerBase)?.ValidationFailed(MyEventContext.Current.Sender.Value, true, reason, false);

        return false;
    }
}
#endif
