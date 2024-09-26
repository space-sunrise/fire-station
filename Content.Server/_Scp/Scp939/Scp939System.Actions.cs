using Content.Shared._Scp.Scp939;
using Content.Shared.Bed.Sleep;
using Content.Shared.Coordinates.Helpers;

namespace Content.Server._Scp.Scp939;

public sealed partial class Scp939System
{
    private static string SleepStatusKey = "Hibernation";

    private void InitializeActions()
    {
        SubscribeLocalEvent<Scp939Component, Scp939SleepAction>(OnSleepAction);
        SubscribeLocalEvent<Scp939Component, Scp939GasAction>(OnGasAction);
    }

    private void OnSleepAction(Entity<Scp939Component> ent, ref Scp939SleepAction args)
    {
        args.Handled = _sleepingSystem.TrySleeping(ent.Owner);

        var hibernationDuration = TimeSpan.FromSeconds(ent.Comp.HibernationDuration);

        _statusEffectsSystem.TryAddStatusEffect<ForcedSleepingComponent>(ent, SleepStatusKey, hibernationDuration, false);
    }

    private void OnGasAction(Entity<Scp939Component> ent, ref Scp939GasAction args)
    {
        var xform = Transform(ent);
        var smokeEntity = Spawn(ent.Comp.SmokeProtoId, xform.Coordinates.SnapToGrid());

        _smokeSystem.StartSmoke(smokeEntity, ent.Comp.SmokeSolution, ent.Comp.SmokeDuration, ent.Comp.SmokeSpreadRadius);

        args.Handled = true;
    }
}
