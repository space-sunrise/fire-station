using Content.Shared.Doors.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Scp.Other.OpenDoorsOnCollide;

public sealed class OpenDoorsOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _door = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OpenDoorsOnCollideComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<OpenDoorsOnCollideComponent> ent, ref StartCollideEvent args)
    {
        _door.StartOpening(args.OtherEntity, null, ent, true);
    }
}
