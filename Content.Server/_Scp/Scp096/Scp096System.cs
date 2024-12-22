using Content.Server.Defusable.WireActions;
using Content.Server.Doors.Systems;
using Content.Server.Power;
using Content.Server.Wires;
using Content.Shared._Scp.Scp096;
using Content.Shared.Doors.Components;
using Content.Shared.Wires;
using Robust.Server.Audio;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly SharedWiresSystem _wiresSystem = default!;
    [Dependency] private readonly DoorSystem _doorSystem = default!;
    [Dependency] private readonly AudioSystem _audioSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        InitTargets();
    }

    protected override void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity)
    {
        base.HandleDoorCollision(scpEntity, doorEntity);

        if (TryComp<DoorBoltComponent>(doorEntity, out var doorBoltComponent))
        {
            _doorSystem.SetBoltsDown(new(doorEntity, doorBoltComponent), true);
        }

        if (!TryComp<WiresComponent>(doorEntity, out var wiresComponent))
            return;

        if (TryComp<WiresPanelComponent>(doorEntity, out var wiresPanelComponent))
        {
            _wiresSystem.TogglePanel(doorEntity, wiresPanelComponent, true);
        }

        foreach (var x in wiresComponent.WiresList)
        {
            if (x.Action is PowerWireAction or BoltWireAction) //Always cut this wires
            {
                x.Action?.Cut(EntityUid.Invalid, x);
            }
            else if (_random.Prob(scpEntity.Comp.WireCutChance)) // randomly cut other wires
            {
                x.Action?.Cut(EntityUid.Invalid, x);
            }
        }

        _audioSystem.PlayPvs(scpEntity.Comp.DoorSmashSoundCollection, doorEntity);
    }


}
