﻿using System.Numerics;
using Content.Shared._Scp.Blood;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;

namespace Content.Client._Scp.Blood;

public sealed class BloodSplatterSystem : SharedBloodSplatterSystem
{
    [Dependency] private readonly AnimationPlayerSystem _animation = default!;

    private const string ParticleAnimationKey = "blood_particle_key";
    private const string BloodLineAnimationKey = "blood_line_key";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BloodParticleComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<BloodLineAnimatedComponent, ComponentStartup>(OnBloodLineInit);
    }

    private void OnInit(Entity<BloodParticleComponent> ent, ref ComponentInit args)
    {
        if (_animation.HasRunningAnimation(ent, ParticleAnimationKey))
            return;

        _animation.Play(ent, GetDropletAnimation(ent), ParticleAnimationKey);
    }

    private void OnBloodLineInit(Entity<BloodLineAnimatedComponent> ent, ref ComponentStartup args)
    {
        if (_animation.HasRunningAnimation(ent, BloodLineAnimationKey))
            return;

        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        // Виздены все сломали в пизду, Color у спрайта не меняется.
        //_animation.Play(ent, GetBloodLineAnimation(sprite), BloodLineAnimationKey);
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
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, 1), 0.225f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, length.Seconds),
                    },
                },
            },
        };
    }


    private static Animation GetBloodLineAnimation(SpriteComponent component)
    {
        var length = BloodLineAnimatedComponent.AnimationDuration;
        var color = component.Color;

        return new Animation
        {
            Length = length,
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Color),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(color.WithAlpha(0f), 0f),
                        new AnimationTrackProperty.KeyFrame(color.WithAlpha(color.A), length.Seconds),
                    },
                },
            },
        };
    }
}
