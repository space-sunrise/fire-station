using Robust.Shared.Prototypes;

namespace Content.Shared.EntityEffects.Effects.Water;

public sealed partial class HitByWaterEntityEffectSystem : EntityEffectSystem<TransformComponent, HitByWater>
{
    protected override void Effect(Entity<TransformComponent> entity, ref EntityEffectEvent<HitByWater> args)
    {
        var ev = new HitByWaterEvent(args.User);

        RaiseLocalEvent(entity, ref ev);
    }
}

/// <summary>
/// Вызывает событие о том, что на сущность попала вода.
/// </summary>
public sealed partial class HitByWater : EntityEffectBase<HitByWater>
{
    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return null;
    }
}

/// <param name="User">Сущность, которая облила водой целевую сущность</param>
[ByRefEvent]
public readonly record struct HitByWaterEvent(EntityUid? User);
