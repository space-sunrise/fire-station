using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Sticky.Components;
using Content.Shared.Trigger;
using Robust.Shared.Timing;
using Robust.Shared.Network;

namespace Content.Shared._Scp.Scp133;

public sealed class Scp133System : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Scp133Component, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, Scp133Component component, TriggerEvent args)
    {
        if (!TryComp<StickyComponent>(uid, out var sticky) || sticky.StuckTo == null)
            return;
        TryActivate(uid, component, sticky.StuckTo.Value);
    }

    public bool TryActivate(EntityUid uid, Scp133Component component, EntityUid target)
    {
        if (!CanActivate(uid, component, target))
            return false;
        var delayTime = TimeSpan.FromSeconds(component.Delay);
        Timer.Spawn(delayTime, () =>
        {
            if (!Exists(target) || !Exists(uid))
                return;
            PerformActivation(uid, component, target);
        });
        return true;
    }

    private bool CanActivate(EntityUid uid, Scp133Component component, EntityUid target)
    {
        return Exists(target) && Exists(uid);
    }

    private void PerformActivation(EntityUid uid, Scp133Component component, EntityUid target)
    {
        _damageable.TryChangeDamage(target, component.Damage, ignoreResistances: true);
        if (component.DeleteAfter)
            QueueDel(uid);
    }
}
