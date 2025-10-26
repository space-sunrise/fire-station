using Robust.Shared.Timing;

namespace Content.Shared._Scp.Animations.Scale;

/// <summary>
/// Система, обрабатывающая анимацию увеличения размера спрайта при появлении сущности.
/// Основывается на <see cref="ScaleAnimationComponent"/>
/// </summary>
public abstract class SharedScaleAnimationSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleAnimationComponent, ComponentInit>(OnInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Убираем компонент с тех сущностей, что уже проиграли свою анимацию.
        var query = EntityQueryEnumerator<ScaleAnimationComponent>();
        while (query.MoveNext(out var uid, out var animation))
        {
            if (!animation.AnimationEndTime.HasValue)
                continue;

            if (Timing.CurTime < animation.AnimationEndTime)
                continue;

            var ev = new ScaleAnimationStopEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void OnInit(Entity<ScaleAnimationComponent> ent, ref ComponentInit args)
    {
        ent.Comp.AnimationEndTime = Timing.CurTime + ent.Comp.Duration;

        var ev = new ScaleAnimationStartEvent();
        RaiseLocalEvent(ent, ref ev);
    }
}

[ByRefEvent]
public readonly record struct  ScaleAnimationStartEvent;

[ByRefEvent]
public readonly record struct ScaleAnimationStopEvent;
