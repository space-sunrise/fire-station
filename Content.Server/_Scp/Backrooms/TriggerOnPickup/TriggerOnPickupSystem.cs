﻿using Content.Server.Explosion.EntitySystems;
using Content.Shared.Item;

namespace Content.Server._Scp.Backrooms.TriggerOnPickup;

public sealed class TriggerOnPickupSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnPickupComponent, GettingPickedUpAttemptEvent>(OnPickUp);
    }

    private void OnPickUp(Entity<TriggerOnPickupComponent> entity, ref GettingPickedUpAttemptEvent args)
    {
        _trigger.Trigger(entity, args.User);
    }
}
