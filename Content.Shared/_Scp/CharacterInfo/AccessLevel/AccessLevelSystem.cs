namespace Content.Shared._Scp.CharacterInfo.AccessLevel;

public sealed class AccessLevelSystem : EntitySystem
{
    /// <summary>
    /// Получает локализованное название уровня доступа персонажа
    /// </summary>
    public string GetName(AccessLevel accessLevel) => accessLevel switch
    {
        AccessLevel.Zero => Loc.GetString("access-level-name-zero"),
        AccessLevel.One => Loc.GetString("access-level-name-one"),
        AccessLevel.Two => Loc.GetString("access-level-name-two"),
        AccessLevel.Three => Loc.GetString("access-level-name-three"),
        AccessLevel.Four => Loc.GetString("access-level-name-four"),
        AccessLevel.Five => Loc.GetString("access-level-name-five"),
        _ => Loc.GetString("access-level-name-unspecified"),
    };

    /// <summary>
    /// Получает локализованное описание уровня доступа персонажа
    /// </summary>
    public string GetDescription(AccessLevel accessLevel) => accessLevel switch
    {
        AccessLevel.Zero => Loc.GetString("access-level-description-zero"),
        AccessLevel.One => Loc.GetString("access-level-description-one"),
        AccessLevel.Two => Loc.GetString("access-level-description-two"),
        AccessLevel.Three => Loc.GetString("access-level-description-three"),
        AccessLevel.Four => Loc.GetString("access-level-description-four"),
        AccessLevel.Five => Loc.GetString("access-level-description-five"),
        _ => Loc.GetString("access-level-description-unspecified"),
    };

    /// <summary>
    /// <inheritdoc cref="GetName"/>
    /// </summary>
    public bool TryGetName(Entity<AccessLevelComponent?> ent, out string name, bool logErrors = true)
    {
        name = string.Empty;

        if (!Resolve(ent, ref ent.Comp, logErrors))
            return false;

        name = GetName(ent.Comp.Level);
        return true;
    }

    /// <summary>
    /// <inheritdoc cref="GetDescription"/>
    /// </summary>
    public bool TryGetDescription(Entity<AccessLevelComponent?> ent, out string description, bool logErrors = true)
    {
        description = string.Empty;

        if (!Resolve(ent, ref ent.Comp, logErrors))
            return false;

        description = GetDescription(ent.Comp.Level);
        return true;
    }
}
