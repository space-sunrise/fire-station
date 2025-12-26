using Content.Shared.Construction;
using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;

namespace Content.Server._Scp.Construction.Conditions;

[UsedImplicitly, DataDefinition]
public sealed partial class HasDamage : IGraphCondition
{
    /// <summary>
    /// Требуется ли урон, чтобы начать данное действие.
    /// Если требуется, то без урона действие не пройдет.
    /// Если не требуется, то с наличием урона действие не пройдет.
    /// </summary>
    [DataField]
    public bool Require;

    public bool Condition(EntityUid uid, IEntityManager entityManager)
    {
        return Require == HasAnyDamage(uid, entityManager);
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var hasAnyDamage = HasAnyDamage(args.Examined);

        switch (Require)
        {
            case true when !hasAnyDamage:
                args.PushMarkup(Loc.GetString("construction-examine-condition-entity-has-damage"));
                return true;
            case false when hasAnyDamage:
                args.PushMarkup(Loc.GetString("construction-examine-condition-entity-has-not-damage"));
                return true;
        }

        return false;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry
        {
            Localization = Require
                ? "construction-step-condition-entity-has-damage"
                : "construction-step-condition-entity-has-not-damage",
        };
    }

    private static bool HasAnyDamage(EntityUid uid, IEntityManager? entityManager = null)
    {
        entityManager ??= IoCManager.Resolve<IEntityManager>();
        if (!entityManager.TryGetComponent<DamageableComponent>(uid, out var damageable))
            return false;

        return damageable.TotalDamage != FixedPoint2.Zero;
    }
}
