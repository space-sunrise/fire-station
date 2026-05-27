using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Sticky.Components;
using Content.Shared.Trigger;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp133;

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

        var target = sticky.StuckTo.Value;
        var delayTime = TimeSpan.FromSeconds(component.Delay);

        Timer.Spawn(delayTime, () =>
        {
            if (!Exists(target) || !Exists(uid))
                return;

            _damageable.TryChangeDamage(target, component.Damage, ignoreResistances: true);

            if (component.DeleteAfter)
                QueueDel(uid);
        });
    }
}