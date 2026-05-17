using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Маркер для существ, которые могут быть жертвами SCP-933 (наклейка ленты, срыв лица).
/// Добавляется через прототипы (например, на базовый прототип гуманоида).
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933PossibleTargetComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanBeFaceTorn = true;

    [DataField, AutoNetworkedField]
    public bool CanWearTape = true;
}
