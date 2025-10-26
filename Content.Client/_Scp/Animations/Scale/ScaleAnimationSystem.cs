using System.Numerics;
using Content.Shared._Scp.Animations.Scale;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Scp.Animations.Scale;

/// <summary>
/// Система, обрабатывающая анимацию увеличения размера спрайта при появлении сущности.
/// Основывается на <see cref="ScaleAnimationComponent"/>
/// </summary>
public sealed class ScaleAnimationSystem : SharedScaleAnimationSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleAnimationComponent, ScaleAnimationStartEvent>(OnStart);
        SubscribeLocalEvent<ScaleAnimationComponent, ScaleAnimationStopEvent>(OnStop);
    }

    private void OnStart(Entity<ScaleAnimationComponent> ent, ref ScaleAnimationStartEvent args)
    {
        if (_animation.HasRunningAnimation(ent, ent.Comp.AnimationKey))
            return;

        var sprite = Comp<SpriteComponent>(ent);
        _animation.Play(ent, GetPuddleAnimation(ent, sprite), ent.Comp.AnimationKey);
    }

    private void OnStop(Entity<ScaleAnimationComponent> ent, ref ScaleAnimationStopEvent args)
    {
        _animation.Stop(ent, null, ent.Comp.AnimationKey);
        _sprite.SetScale(ent.Owner, Vector2.One);
    }

    private static Animation GetPuddleAnimation(Entity<ScaleAnimationComponent> ent, SpriteComponent component)
    {
        var length = ent.Comp.Duration;
        var scale = component.Scale;

        return new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Scale),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(ent.Comp.InitialSize, 0f),
                        new AnimationTrackProperty.KeyFrame(scale, length.Seconds),
                    },
                },
            },
        };
    }
}
