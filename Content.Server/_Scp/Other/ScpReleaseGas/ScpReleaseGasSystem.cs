using Content.Server.Actions;
using Content.Shared._Scp.Other.ScpReleaseGas;
using Content.Shared._Scp.ScpMask;
using Content.Shared.Trigger;

namespace Content.Server._Scp.Other.ScpReleaseGas;

public sealed class ScpReleaseGasSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;

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
        if (args.Key == null)
            return;

        if (!ent.Comp.TriggerKeys.Contains(args.Key))
            return;

        if (_scpMask.TryGetScpMask(ent, out var scpMask))
        {
            _scpMask.TryCreatePopup(ent, scpMask);
            args.Cancelled = true;
            return;
        }
    }
}
