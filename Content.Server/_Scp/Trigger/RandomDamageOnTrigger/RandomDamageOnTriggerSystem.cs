using Content.Server.Destructible;
using Content.Shared._Scp.Trigger.RandomDamageOnTrigger;
using Content.Shared.Damage;
using Content.Shared.Trigger;
using Robust.Shared.Random;

namespace Content.Server._Scp.Trigger.RandomDamageOnTrigger;

public sealed class RandomDamageOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DestructibleSystem _destructible = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly DamageSpecifier _tempDamage = new ();
    private const double Lambda = 2.5d;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomDamageOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RandomDamageOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        if (!_random.Prob(ent.Comp.Probability))
            return;

        if (!_destructible.TryGetDestroyedAt(ent.Owner, out var destroyed))
        {
            Log.Warning($"Tried to trigger {nameof(RandomDamageOnTriggerComponent)} for entity without {nameof(DestructibleComponent)}! " +
                        $"Entity is {ToPrettyString(ent)}, Prototype: {Prototype(ent)}");
            return;
        }

        foreach (var type in ent.Comp.DamageTypes)
        {
            // Экспоненциальное распределение, чтобы более маленькие значения встречались чаще, чем большие.
            // Это нужно, чтобы весь комплекс не утонул в полуразрушенных стенах, но возможность их увидеть оставалась.
            var modifier = 1 - Math.Exp(-Lambda * _random.NextDouble(ent.Comp.MinDamagePercent, ent.Comp.MaxDamagePercent));
            _tempDamage.DamageDict[type] = destroyed.Value * modifier;
        }

        _damageable.TryChangeDamage(ent, _tempDamage, ent.Comp.IgnoreResistancesForDamage);
        _tempDamage.DamageDict.Clear();
    }
}
