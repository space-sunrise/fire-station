using System.Numerics;
using Content.Shared.Drowsiness;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Effects;

public sealed partial class DrowsinessStatusEffect : EntityEffect
{
    [DataField]
    public int MinAccidentTime { get; set; } = 20;

    [DataField]
    public int MaxAccidentTime { get; set; } = 40;

    [DataField]
    public int MinSleepTime { get; set; } = 20;

    [DataField]
    public int MaxSleepTime { get; set; } = 40;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return string.Empty;
    }

    public override void Effect(EntityEffectBaseArgs args)
    {
        var target = args.TargetEntity;

        if (args.EntityManager.HasComponent<DrowsinessComponent>(target))
        {
            return;
        }

        var drowsiness = new DrowsinessComponent
        {
            DurationOfIncident = new Vector2(MinSleepTime, MaxSleepTime),
            TimeBetweenIncidents = new Vector2(MaxAccidentTime, MaxAccidentTime)
        };

        args.EntityManager.AddComponent(target, drowsiness);
    }
}
