using Content.Server.Actions;
using Content.Shared._Scp.Other.ScpReleaseGas;
using Content.Shared._Scp.Other.Events;
using Content.Shared._Scp.ScpMask;
using Content.Shared.Trigger.Systems;

namespace Content.Server._Scp.Other.ScpReleaseGas;

public sealed class ScpReleaseGasSystem : EntitySystem
{
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpReleaseGasComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ScpReleaseGasComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ScpReleaseGasComponent, ScpReleaseGasActionAttemptEvent>(OnGasActionAttempt);
        SubscribeLocalEvent<ScpReleaseGasComponent, ScpReleaseGasActionEvent>(OnGasAction);
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

    public void OnGasActionAttempt(Entity<ScpReleaseGasComponent> ent, ref ScpReleaseGasActionAttemptEvent args)
    {
        if (_scpMask.TryGetScpMask(ent, out var scpMask))
        {
            _scpMask.TryCreatePopup(ent, scpMask);
            args.Cancel();
            return;
        }
    }

    public void OnGasAction(Entity<ScpReleaseGasComponent> ent, ref ScpReleaseGasActionEvent args)
    {
        var ev = new ScpReleaseGasActionAttemptEvent();
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return;

        foreach (var key in ent.Comp.KeysOut)
            _trigger.Trigger(ent, args.Performer, key, false);

        args.Handled = true;
    }
}
