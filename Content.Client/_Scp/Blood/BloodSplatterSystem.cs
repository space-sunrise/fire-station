using System.Numerics;
using Content.Shared._Scp.Blood;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Scp.Blood;

public sealed partial class BloodSplatterSystem : SharedBloodSplatterSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string ParticleAnimationKey = "blood_particle_key";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodParticleAnimationStartEvent>(OnStart);
    }

    private void OnStart(BloodParticleAnimationStartEvent args)
    {
        var ent = GetEntity(args.Entity);
        if (!TryComp<BloodParticleComponent>(ent, out var particle))
            return;

        if (_animation.HasRunningAnimation(ent, ParticleAnimationKey))
            return;

        _animation.Play(ent, GetDropletAnimation((ent, particle)), ParticleAnimationKey);
    }

    private static Animation GetDropletAnimation(Entity<BloodParticleComponent> ent)
    {
        var length = ent.Comp.FlyTime;
        return new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0f, 1f), 0.225f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length.Seconds),
                    },
                },
            },
        };
    }
}
