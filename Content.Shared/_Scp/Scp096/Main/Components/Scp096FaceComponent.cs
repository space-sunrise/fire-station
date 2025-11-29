using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

// TODO: Верб для открытия VV лица для админов
/// <summary>
/// Компонент, отвечающий за определение лица скромника.
/// Содержит ссылку на владельца лица.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096FaceComponent : Component
{
    /// <summary>
    /// Владелец лица(скромник)
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? FaceOwner;

    [DataField]
    public ProtoId<ReagentPrototype> TearsReagent = "Scp096Tears";

    [DataField]
    public ProtoId<ReagentPrototype> BloodReagent = "Blood";

    [DataField]
    public float LiquidSpawnCooldownDivisor = 3f;

    [ViewVariables]
    public TimeSpan? CachedLiquidSpawnCooldown;
}
