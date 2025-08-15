using Robust.Shared.GameStates;

namespace Content.Shared._Scp.CharacterInfo.EmployeeClass;

/// <summary>
/// Компонент, отвечающий за информацию о классе персонажа.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class EmployeeClassComponent : Component
{
    [DataField, AutoNetworkedField]
    public EmployeeClass Class = EmployeeClass.Unspecified;

    [ViewVariables]
    public static readonly Color InfoColor = Color.FromHex("#A1A1A1");
}

/// <summary>
/// Класс персонажа.
/// https://scpfoundation.net/security-clearance-levels#toc7
/// </summary>
public enum EmployeeClass : byte
{
    A,
    B,
    C,
    D,
    E,
    Unspecified,
}
