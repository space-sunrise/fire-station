using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp247;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp247Component : Component
{
    /// <summary>
    /// Время непрерывного наблюдения, после которого для наблюдателя открывается истинный облик.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RevealTime = TimeSpan.FromMinutes(8);

    /// <summary>
    /// Время без наблюдения, после которого прогресс наблюдателя сбрасывается.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan ResetTime = TimeSpan.FromSeconds(24);

    /// <summary>
    /// Сущности, которых SCP-247 уже атаковал и которым разрешено причинять ему вред.
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> AllowedAttackers = [];
}

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp247ProtectionComponent : Component;
