using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Random.Rules;
using JetBrains.Annotations;

namespace Content.Shared._Scp.Other.Rules;

[UsedImplicitly]
public sealed partial class HasDamageRule : RulesRule
{
    /// <summary>
    /// Требуется ли урон, чтобы начать данное действие.
    /// Если требуется, то без урона действие не пройдет.
    /// Если не требуется, то с наличием урона действие не пройдет.
    /// </summary>
    [DataField]
    public bool Require;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        return Require == HasAnyDamage(uid, entManager);
    }

    private static bool HasAnyDamage(EntityUid uid, EntityManager entManager)
    {
        if (!entManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
            return false;

        return damageable.TotalDamage != FixedPoint2.Zero;
    }
}
