using Content.Server.Actions;
using Content.Server.Fluids.EntitySystems;
using Content.Shared._Scp.Other.ScpReleaseGas;
using Content.Shared._Scp.ScpMask;
using Content.Shared.Coordinates.Helpers;

namespace Content.Server._Scp.Scp939;

public sealed class ScpReleaseGasSystem : EntitySystem
{
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly SmokeSystem _smokeSystem = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpReleaseGasComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ScpReleaseGasComponent, ScpReleaseGasActionEvent>(OnGasAction);
    }

    public void OnInit(Entity<ScpReleaseGasComponent> ent, ref ComponentInit args)
    {
        _actionsSystem.AddAction(ent, ent.Comp.ActionProto);
    }

    private void OnGasAction(Entity<ScpReleaseGasComponent> ent, ref ScpReleaseGasActionEvent args)
    {
        if (_scpMask.TryGetScpMask(ent, out var scpMask))
        {
            _scpMask.TryCreatePopup(ent, scpMask);
            return;
        }

        var xform = Transform(ent);
        var smokeEntity = Spawn(ent.Comp.SmokeProtoId, xform.Coordinates.SnapToGrid());

        _smokeSystem.StartSmoke(smokeEntity, ent.Comp.SmokeSolution, ent.Comp.SmokeDuration, ent.Comp.SmokeSpreadRadius);

        args.Handled = true;
    }
}
