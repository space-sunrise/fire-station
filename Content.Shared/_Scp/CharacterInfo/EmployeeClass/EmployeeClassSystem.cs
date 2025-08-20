namespace Content.Shared._Scp.CharacterInfo.EmployeeClass;

public sealed class EmployeeClassSystem : EntitySystem
{
    /// <summary>
    /// Получает локализованное название класса персонажа
    /// </summary>
    public string GetName(EmployeeClass? employeeClass) => employeeClass switch
    {
        EmployeeClass.A => Loc.GetString("employee-class-name-a"),
        EmployeeClass.B => Loc.GetString("employee-class-name-b"),
        EmployeeClass.C => Loc.GetString("employee-class-name-c"),
        EmployeeClass.D => Loc.GetString("employee-class-name-d"),
        EmployeeClass.E => Loc.GetString("employee-class-name-e"),
        _ => Loc.GetString("employee-class-name-unspecified"),
    };

    /// <summary>
    /// Получает локализованное описание класса персонажа
    /// </summary>
    public string GetDescription(EmployeeClass? employeeClass) => employeeClass switch
    {
        EmployeeClass.A => Loc.GetString("employee-class-description-a"),
        EmployeeClass.B => Loc.GetString("employee-class-description-b"),
        EmployeeClass.C => Loc.GetString("employee-class-description-c"),
        EmployeeClass.D => Loc.GetString("employee-class-description-d"),
        EmployeeClass.E => Loc.GetString("employee-class-description-e"),
        _ => Loc.GetString("employee-class-description-unspecified"),
    };

    /// <summary>
    /// <inheritdoc cref="GetName"/>
    /// </summary>
    public bool TryGetName(Entity<EmployeeClassComponent?> ent, out string name, bool logErrors = true)
    {
        name = string.Empty;

        if (!Resolve(ent, ref ent.Comp, logErrors))
            return false;

        name = GetName(ent.Comp.Class);
        return true;
    }

    /// <summary>
    /// <inheritdoc cref="GetDescription"/>
    /// </summary>
    public bool TryGetDescription(Entity<EmployeeClassComponent?> ent, out string description, bool logErrors = true)
    {
        description = string.Empty;

        if (!Resolve(ent, ref ent.Comp, logErrors))
            return false;

        description = GetDescription(ent.Comp.Class);
        return true;
    }
}
