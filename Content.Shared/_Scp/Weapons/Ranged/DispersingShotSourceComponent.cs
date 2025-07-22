using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DispersingShotSourceComponent : Component
{
    /// <summary>
    /// <inheritdoc cref="AngleIncreaseMultiplier"/>.
    /// Устанавливается только через прототипы.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, Access(typeof(DispersingShotSystem))]
    public float DefaultAngleIncreaseModifier = 2f;

    /// <summary>
    /// <inheritdoc cref="MaxAngleMultiplier"/>.
    /// Устанавливается только через прототипы.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField, Access(typeof(DispersingShotSystem))]
    public float DefaultMaxAngleMultiplier = 2f;

    /// <summary>
    /// Модификатор влияющий на то, как будет увеличиваться угол при каждом выстреле.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float AngleIncreaseMultiplier;

    /// <summary>
    /// Модификатор, влияющий на максимальный угол разброса.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float MaxAngleMultiplier;
}
