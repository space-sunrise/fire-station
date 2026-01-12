using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

// TODO: Верб для открытия VV лица для админов
/// <summary>
/// Компонент, отвечающий за определение лица скромника.
/// Содержит ссылку на владельца лица.
/// <seealso cref="Scp096Component"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096FaceComponent : Component
{
    /// <summary>
    /// Владелец лица(скромник)
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? FaceOwner;

    /// <summary>
    /// Реагент, который будет использован в качестве слез.
    /// Им будет плакать владелец лица в состоянии покоя
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> TearsReagent = "Scp096Tears";

    /// <summary>
    /// Реагент, который будет использоваться в качестве кровавых слез.
    /// Им будет плакать объект в состоянии содранного лица <see cref="ActiveScp096WithoutFaceComponent"/>
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype> BloodReagent = "Scp096Blood";

    /// <summary>
    /// Делитель отката до спавна следующей слезы.
    /// Чем он больше, тем быстрее будут создаваться слезы
    /// </summary>
    [DataField]
    public float LiquidSpawnCooldownDivisor = 3f;

    /// <summary>
    /// Сохраненный <see cref="LiquidSpawnCooldownDivisor"/>.
    /// Используется, чтобы восстановиться до прошлого значения после смены значения.
    /// </summary>
    [ViewVariables]
    public TimeSpan? CachedLiquidSpawnCooldown;

    /// <summary>
    /// Сохраненная вариация отката до спавна следующей слезы.
    /// Используется, чтобы восстановиться до прошлого значения после смены значения.
    /// </summary>
    [ViewVariables]
    public TimeSpan? CachedCooldownVariation;
}
