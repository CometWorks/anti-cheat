#if DEDICATED || MAGNETAR
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Shared.Plugin;
using VRage.Game.ModAPI;

namespace Shared.Patches;

/// <summary>
/// Determines whether a connected client (by Steam ID) is authorized to see
/// the script stored in a given Programmable Block.
/// Authorized means: the client meets the configured promote level, is the grid owner,
/// or is in the same faction as the owner.
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

        var minLevel = Common.Config?.ProgrammableBlockScriptMinPromoteLevel ?? MyPromoteLevel.SpaceMaster;
        var userLevel = minLevel != MyPromoteLevel.None
            ? MySession.Static.GetUserPromoteLevel(steamId)
            : MyPromoteLevel.None;

        var ownerId = pb.OwnerId;
        var requesterIdentityId = MySession.Static.Players.TryGetIdentityId(steamId);

        var ownerFaction = MySession.Static.Factions.TryGetPlayerFaction(ownerId);
        var requesterFaction = MySession.Static.Factions.TryGetPlayerFaction(requesterIdentityId);

        return IsAuthorizedCore(
            steamId, minLevel, userLevel,
            ownerId, requesterIdentityId,
            ownerFaction?.FactionId,
            requesterFaction?.FactionId);
    }

    /// <summary>
    /// Pure authorization logic, decoupled from SE statics for testability.
    /// </summary>
    /// <param name="steamId">Steam ID of the requesting client (0 = server/local).</param>
    /// <param name="minLevel">Minimum promote level that grants unconditional access.</param>
    /// <param name="userLevel">The requesting client's current promote level.</param>
    /// <param name="ownerId">Identity ID of the block owner (0 = unowned).</param>
    /// <param name="requesterIdentityId">Identity ID resolved from <paramref name="steamId"/> (0 = not found).</param>
    /// <param name="ownerFactionId">Faction ID of the owner, or null if the owner has no faction.</param>
    /// <param name="requesterFactionId">Faction ID of the requester, or null if they have no faction.</param>
    internal static bool IsAuthorizedCore(
        ulong steamId,
        MyPromoteLevel minLevel,
        MyPromoteLevel userLevel,
        long ownerId,
        long requesterIdentityId,
        long? ownerFactionId,
        long? requesterFactionId)
    {
        // Local/server invocations carry steamId == 0; always allow.
        if (steamId == 0)
            return true;

        // Privileged users bypass ownership/faction checks.
        if (minLevel != MyPromoteLevel.None && userLevel >= minLevel)
            return true;

        // Unowned blocks are not secret.
        if (ownerId == 0)
            return true;

        // Requester identity could not be resolved.
        if (requesterIdentityId == 0)
            return false;

        // Direct owner match.
        if (requesterIdentityId == ownerId)
            return true;

        // Same-faction check.
        if (ownerFactionId == null)
            return false;

        if (requesterFactionId == null)
            return false;

        return ownerFactionId == requesterFactionId;
    }
}
#endif
