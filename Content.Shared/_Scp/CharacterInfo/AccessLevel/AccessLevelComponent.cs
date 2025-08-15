using Robust.Shared.GameStates;

namespace Content.Shared._Scp.CharacterInfo.AccessLevel;

/// <summary>
/// Компонент, отвечающий за информацию об уровне доступа сотрудника.
/// https://scpfoundation.net/security-clearance-levels#toc0
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AccessLevelComponent : Component
{
    [DataField, AutoNetworkedField]
    public AccessLevel Level = AccessLevel.Unspecified;

    [ViewVariables]
    public static readonly Color InfoColor = Color.FromHex("#A1A1A1");
}

/// <summary>
/// Уровень доступа сотрудника.
/// https://scpfoundation.net/security-clearance-levels#toc0
/// </summary>
public enum AccessLevel : byte
{
    Zero,
    One,
    Two,
    Three,
    Four,
    Five,
    Unspecified,
}
