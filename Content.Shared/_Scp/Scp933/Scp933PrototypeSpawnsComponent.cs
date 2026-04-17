using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Прототипы для спавна SCP-933.
/// Все ID прототипов вынесены для кастомизации через YAML.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933PrototypeSpawnsComponent : Component
{
    /// <summary>
    /// Прототип полоски ленты, отрываемой от рулона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId TapeMaskPrototype = "ClothingMaskScp933Tape";

    /// <summary>
    /// Прототип рулона ленты (для спавна если нужно).
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? TapeRollPrototype;

    /// <summary>
    /// Прототип эффекта при появлении хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? HostEmergeEffectPrototype;

    /// <summary>
    /// Прототип эффекта при срыве лица.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? FaceTornEffectPrototype;
}
