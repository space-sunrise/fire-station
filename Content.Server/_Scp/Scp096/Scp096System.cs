using Content.Server.Defusable.WireActions;
using Content.Server.Doors.Systems;
using Content.Server.Power;
using Content.Server.Wires;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;
using Robust.Server.Audio;
using Robust.Server.GameStates;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly WiresSystem _wires = default!;
    [Dependency] private readonly DoorSystem _door = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    // TODO: Переделать это под отдельный компонент, который будет выдаваться и убираться
    protected override void HandleDoorCollision(Entity<Scp096Component> scp, Entity<DoorComponent> door)
    {
        base.HandleDoorCollision(scp, door);

        if (!scp.Comp.InRageMode)
            return;

        _door.StartOpening(door);

        if (TryComp<DoorBoltComponent>(door, out var doorBoltComponent))
        {
            _door.SetBoltsDown((door, doorBoltComponent), true);
        }

        if (!TryComp<WiresComponent>(door, out var wiresComponent))
            return;

        if (TryComp<WiresPanelComponent>(door, out var wiresPanelComponent))
        {
            _wires.TogglePanel(door, wiresPanelComponent, true);
        }

        foreach (var x in wiresComponent.WiresList)
        {
            if (x.Action is PowerWireAction or BoltWireAction) //Always cut this wires
            {
                x.Action?.Cut(EntityUid.Invalid, x);
            }
            else if (_random.Prob(scp.Comp.WireCutChance)) // randomly cut other wires
            {
                x.Action?.Cut(EntityUid.Invalid, x);
            }
        }

        _audio.PlayPvs(scp.Comp.DoorSmashSoundCollection, door);
    }

    protected override void AddTarget(Entity<Scp096Component> scp, EntityUid target)
    {
        base.AddTarget(scp, target);

        _pvsOverride.AddGlobalOverride(target);
    }

    protected override void RemoveTarget(Entity<Scp096Component> scp, Entity<Scp096TargetComponent?> target, bool removeComponent = true)
    {
        base.RemoveTarget(scp, target, removeComponent);

        _pvsOverride.RemoveGlobalOverride(target);
    }
}
