using Content.Server.Actions;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server._Scp.Scp082;

public sealed class Scp082System : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp082Component, MapInitEvent>(OnInit);
    }

    private void OnInit(EntityUid uid, Scp082Component component, MapInitEvent args)
    {
    }
}
