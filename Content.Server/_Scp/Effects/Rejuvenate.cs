using Content.Server.Administration.Systems;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;
using Serilog;

namespace Content.Server._Scp.Effects;

public sealed partial class Rejuvenate : EntityEffect
{
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototypeManager,
        IEntitySystemManager entitySystemManager)
    {
        return Loc.GetString("reagent-effect-guidebook-scp500");
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        Log.Debug($"rejuvenated {args.TargetEntity}");
        args.EntityManager.System<RejuvenateSystem>().PerformRejuvenate(args.TargetEntity);
    }
}
