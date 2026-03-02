using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.EntityEffects;

[UsedImplicitly]
public sealed partial class CalmDownEffect : EntityEffectBase<CalmDownEffect>
{
    [DataField]
    public TimeSpan SpeedUpBy = TimeSpan.FromSeconds(5f);

    public override string EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) =>
        Loc.GetString("reagent-effect-guidebook-calm-down");
}
