#if DEDICATED || MAGNETAR
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using VRage.Game;

namespace Shared.Patches;

/// <summary>
/// Determines whether a connected client (by Steam ID) is authorized to see
/// the script stored in a given Programmable Block.
/// Mirrors the game's own terminal access rule: owner, faction share (which also
/// covers share-with-all), unowned blocks, or an admin with UseTerminals.
/// </summary>
internal static class ScriptAccessHelper
{
    /// <param name="pb">The Programmable Block whose script is being guarded.</param>
    /// <param name="steamId">Steam ID of the client requesting access.</param>
    /// <returns>True if the client may receive the script contents.</returns>
    public static bool IsAuthorized(MyProgrammableBlock pb, ulong steamId)
    {
        if (steamId == 0)
            return true;

        var identityId = MySession.Static.Players.TryGetIdentityId(steamId);
        if (identityId == 0)
            return false;

        return IsAuthorizedCore(pb.GetUserRelationToOwner(identityId), pb.HasAdminUseTerminals(identityId));
    }

    /// <summary>
    /// Pure authorization logic, decoupled from SE statics for testability.
    /// </summary>
    /// <param name="relation">The requester's relation to the block owner.</param>
    /// <param name="hasAdminUseTerminals">Whether the requester has the UseTerminals admin permission.</param>
    internal static bool IsAuthorizedCore(MyRelationsBetweenPlayerAndBlock relation, bool hasAdminUseTerminals)
    {
        if (hasAdminUseTerminals)
            return true;

        return relation == MyRelationsBetweenPlayerAndBlock.Owner
               || relation == MyRelationsBetweenPlayerAndBlock.FactionShare
               || relation == MyRelationsBetweenPlayerAndBlock.NoOwnership;
    }
}
#endif
