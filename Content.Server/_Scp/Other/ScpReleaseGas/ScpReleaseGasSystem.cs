using Content.Server.Actions;
using Content.Shared._Scp.Other.ScpReleaseGas;
using Content.Shared.Trigger;

namespace Content.Server._Scp.Other.ScpReleaseGas;

public sealed class ScpReleaseGasSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpReleaseGasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScpReleaseGasComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ScpReleaseGasComponent, AttemptTriggerEvent>(OnAttemptTriggerEvent);
    }

    private void OnMapInit(Entity<ScpReleaseGasComponent> ent, ref MapInitEvent args)
    {
        var actionEnt = _actionsSystem.AddAction(ent, ent.Comp.ActionProto);
        ent.Comp.ActionEnt = actionEnt;
    }

    private void OnShutdown(Entity<ScpReleaseGasComponent> ent, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(ent.Owner, ent.Comp.ActionEnt);
        ent.Comp.ActionEnt = null;
    }

    private void OnAttemptTriggerEvent(Entity<ScpReleaseGasComponent> ent, ref AttemptTriggerEvent args)
    {

    }
}
