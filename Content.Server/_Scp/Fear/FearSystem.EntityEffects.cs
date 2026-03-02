using Content.Shared._Scp.EntityEffects;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.EntityEffects;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    private void InitializeEntityEffects()
    {
        SubscribeLocalEvent<FearComponent, EntityEffectEvent<CalmDownEffect>>(OnExecuteCalmDown);
    }

    private void OnExecuteCalmDown(Entity<FearComponent> ent, ref EntityEffectEvent<CalmDownEffect> args)
    {
        ent.Comp.NextTimeDecreaseFearLevel -= args.Effect.SpeedUpBy;
        Dirty(ent);
    }
}
