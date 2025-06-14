using Content.Shared.EntityEffects;
using Content.Shared.Rejuvenate;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Effects;

public sealed partial class Rejuvenate : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototypeManager, IEntitySystemManager entitySystemManager)
    {
        return Loc.GetString("reagent-effect-guidebook-scp500");
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.EventBus.RaiseLocalEvent(args.TargetEntity, new RejuvenateEvent());
    }
}
