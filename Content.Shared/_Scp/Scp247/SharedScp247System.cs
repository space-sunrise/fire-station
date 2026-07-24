using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Watching;
using Content.Shared.Damage.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Interaction.Events;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp247;

public abstract class SharedScp247System : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, EntityLookedAtEvent>(OnEntityLookedAt);
        SubscribeLocalEvent<AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp247Component, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
        SubscribeLocalEvent<MeleeWeaponComponent, MeleeHitEvent>(OnMeleeHit);
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var currentTime = _timing.CurTime;
        var query = EntityQueryEnumerator<Scp247Component>();
        while (query.MoveNext(out var uid, out var scp247))
        {
            for (var i = scp247.Watchers.Count - 1; i >= 0; i--)
            {
                var state = scp247.Watchers[i];
                if (currentTime - state.LastSeen > scp247.WatchGracePeriod)
                {
                    scp247.Watchers.RemoveAt(i);
                    continue;
                }

                if (state.Triggered || currentTime < state.StartedAt + scp247.RequiredWatchTime)
                    continue;

                state.Triggered = true;
                OnWatchTimeReached((uid, scp247), state.Viewer);
            }
        }
    }

    protected virtual void OnWatchTimeReached(Entity<Scp247Component> target, EntityUid viewer)
    {
    }

    private void OnEntityLookedAt(Entity<BlinkableComponent> viewer, ref EntityLookedAtEvent args)
    {
        if (!TryComp<Scp247Component>(args.Target, out var scp247))
            return;

        var currentTime = _timing.CurTime;
        foreach (var state in scp247.Watchers)
        {
            if (state.Viewer != viewer.Owner)
                continue;

            if (currentTime - state.LastSeen > scp247.WatchGracePeriod)
            {
                state.StartedAt = currentTime;
                state.Triggered = false;
            }

            state.LastSeen = currentTime;
            return;
        }

        scp247.Watchers.Add(new Scp247WatchState(viewer.Owner, currentTime, currentTime));
    }

    private void OnAttackAttempt(AttackAttemptEvent args)
    {
        Log.Info("ATACK ATTEMPT");
        if (!args.Target.HasValue || !TryComp<Scp247Component>(args.Target.Value, out var scp247))
            return;

        if (CanIgnoreEffect(args.Uid, scp247))
            return;

        if (!IsActivelyWatching(args.Uid, args.Target.Value, scp247))
            return;

        args.Cancel();
    }

    private void OnBeforeDamageChanged(Entity<Scp247Component> target, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin is not { } attacker)
            return;

        if (CanIgnoreEffect(attacker, target.Comp))
            return;

        if (!IsActivelyWatching(attacker, target.Owner, target.Comp))
            return;

        args.Cancelled = true;
    }

    private void OnMeleeHit(Entity<MeleeWeaponComponent> _, ref MeleeHitEvent args)
    {
        if (!args.IsHit || !TryComp<Scp247Component>(args.User, out var scp247))
            return;

        var changed = false;
        foreach (var target in args.HitEntities)
        {
            if (target == args.User)
                continue;

            changed |= scp247.AttackedViewers.Add(GetNetEntity(target));
        }

        if (changed)
            Dirty(args.User, scp247);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        var query = EntityQueryEnumerator<Scp247Component>();
        while (query.MoveNext(out _, out var scp247))
        {
            scp247.Watchers.Clear();
            scp247.AngryForLocalViewer = false;
            scp247.RenderedState = null;
        }
    }

    private bool IsActivelyWatching(EntityUid viewer, EntityUid target, Scp247Component scp247)
    {
        foreach (var state in scp247.Watchers)
        {
            if (state.Viewer == viewer)
                return _timing.CurTime - state.LastSeen <= scp247.WatchGracePeriod;
        }

        return false;
    }

    private bool CanIgnoreEffect(EntityUid viewer, Scp247Component scp247)
    {
        if (HasComp<Scp247ProtectionComponent>(viewer))
            return true;

        return scp247.AttackedViewers.Contains(GetNetEntity(viewer));
    }

}
