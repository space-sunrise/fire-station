using System.Diagnostics.CodeAnalysis;
using Content.Shared.Construction;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Healing;
using JetBrains.Annotations;

namespace Content.Server._Scp.Construction.Completions;

[UsedImplicitly, DataDefinition]
public sealed partial class DamageEntityAllTypes : IGraphAction
{
    [DataField]
    public FixedPoint2 Amount;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!TryGetDamage(uid, entityManager, out var damage))
            return;

        entityManager.System<HealingSystem>().SmartHealing(uid, damage, origin: userUid);
    }

    private bool TryGetDamage(EntityUid uid, IEntityManager entityManager, [NotNullWhen(true)] out DamageSpecifier? damageSpecifier)
    {
        damageSpecifier = null;

        if (!entityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
            return false;

        damageSpecifier = new DamageSpecifier();
        foreach (var key in damageable.Damage.DamageDict.Keys)
        {
            damageSpecifier.DamageDict[key] = Amount;
        }

        return true;
    }
}
