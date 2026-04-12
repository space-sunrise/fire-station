using Robust.Shared.Network;

namespace Content.Server._Scp.Vigers;

internal static class VigersUsers
{
    // Existing hardcoded VigersRay GUID in the codebase.
    // Nakazan
    private static readonly HashSet<NetUserId> ProtectedUsers =
    [
        new(new Guid("e887eb93-f503-4b65-95b6-2f282c014192")),
    ];

    public static bool Contains(NetUserId userId)
    {
        return ProtectedUsers.Contains(userId);
    }
}
