using Content.Shared._Scp.EntityEffects;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    private void InitializeEntityEffects()
    {
        SubscribeLocalEvent<ExecuteEntityEffectEvent<CalmDownEffect>>(OnExecuteCalmDown);
    }

    private void OnExecuteCalmDown(ref ExecuteEntityEffectEvent<CalmDownEffect> ev)
    {
        if (ev.Args is not EntityEffectReagentArgs args)
            return;

        if (args.Scale == FixedPoint2.Zero)
            return;

        if (!TryComp<FearComponent>(args.TargetEntity, out var fear))
            return;

        var time = ev.Effect.SpeedUpBy / args.Scale.Float();
        fear.NextTimeDecreaseFearLevel -= time;
        Dirty(args.TargetEntity, fear);
    }
}
