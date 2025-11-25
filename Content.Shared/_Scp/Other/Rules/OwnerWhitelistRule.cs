using Content.Shared.Random.Rules;
using Content.Shared.Whitelist;

namespace Content.Shared._Scp.Other.Rules;

/// <summary>
/// Проверяет переданный вайтлист и блеклист для сущности.
/// </summary>
public sealed partial class OwnerWhitelistRule : RulesRule
{
    /// <summary>
    /// Белый список компонентов и тегов, который должна иметь сущность.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Черный список компонентов и тегов, который должна иметь сущность.
    /// </summary>
    [DataField]
    public EntityWhitelist? Blacklist;

    /// <summary>
    /// Система белых список для проверки сущности.
    /// Кешируется в переменную.
    /// </summary>
    private EntityWhitelistSystem? _whitelist;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        _whitelist ??= entManager.System<EntityWhitelistSystem>();

        if (!_whitelist.CheckBoth(uid, Blacklist, Whitelist))
            return Inverted;

        return !Inverted;
    }
}
