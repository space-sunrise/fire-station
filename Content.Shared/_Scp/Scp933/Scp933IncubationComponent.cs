using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Компонент инкубации SCP-933.
/// Таймеры и настройки превращения в хоста.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933IncubationComponent : Component
{
    /// <summary>
    /// Текущее время инкубации.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CurrentIncubationTime = TimeSpan.Zero;

    /// <summary>
    /// Требуемое время инкубации для превращения.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan RequiredIncubationTime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Интервал проверки состояния инкубации.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan CheckInterval = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Может ли инкубация быть прервана.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanBeInterrupted = true;

    /// <summary>
    /// Условия прерывания инкубации.
    /// </summary>
    [DataField, AutoNetworkedField]
    public IncubationInterruptConditions InterruptConditions = IncubationInterruptConditions.Damage | IncubationInterruptConditions.Death;

    /// <summary>
    /// Эффекты во время инкубации.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<IncubationEffect> ActiveEffects = new()
    {
        IncubationEffect.Muted,
        IncubationEffect.BlurredVision,
    };

    /// <summary>
    /// Пороги для эффектов (процент инкубации -> эффект).
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<float, IncubationEffect> EffectThresholds = new()
    {
        { 0.25f, IncubationEffect.SlowMovement },
        { 0.50f, IncubationEffect.BlurredVision },
        { 0.75f, IncubationEffect.Hallucinations },
    };
}

[Flags]
public enum IncubationInterruptConditions : byte
{
    None = 0,
    Damage = 1 << 0,
    Death = 1 << 1,
    TapeRemoval = 1 << 2,
    MedicalTreatment = 1 << 3,
}

[Flags]
public enum IncubationEffect : byte
{
    None = 0,
    Muted = 1 << 0,
    SlowMovement = 1 << 1,
    BlurredVision = 1 << 2,
    Hallucinations = 1 << 3,
    HealthDrain = 1 << 4,
}
