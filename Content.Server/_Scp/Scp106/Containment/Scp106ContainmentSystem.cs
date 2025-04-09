using Content.Server.Chat.Systems;
using Content.Shared._Scp.Scp106.Containment;
using Robust.Shared.Physics.Events;

namespace Content.Server._Scp.Scp106.Containment;

public sealed class Scp106ContainmentSystem : SharedScp106ContainmentSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;

    protected override void OnBoneBreakerCollide(Entity<Scp106BoneBreakerCellComponent> ent, ref StartCollideEvent args)
    {
        base.OnBoneBreakerCollide(ent, ref args);

        _chat.DispatchStationAnnouncement(ent, Loc.GetString("scp106-return-to-containment"));
    }

}
