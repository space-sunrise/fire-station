using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DispersingShotSourceComponent : Component
{
    /// <summary>
    /// Модификатор влияющий на то, как будет увеличиваться угол при каждом выстреле.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public float AngleIncreaseMultiplier = 15f;

    /// <summary>
    /// Модификатор, влияющий на максимальный угол разброса.
    /// </summary>
    [DataField, ViewVariables, AutoNetworkedField]
    public float MaxAngleMultiplier = 15f;
}
