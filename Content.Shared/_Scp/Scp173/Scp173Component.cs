using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp173;

/// <summary>
/// Компонент, отвечающий за способности и ограничения SCP-173.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class Scp173Component : Component
{
    #region Fast movement action

    /// <summary>
    /// Максимальный радиус прыжка
    /// </summary>
    [DataField]
    public float MaxJumpRange = 4f;

    /// <summary>
    /// Максимальное количество смотрящих, которое позволит совершить прыжок
    /// </summary>
    [DataField]
    public int MaxWatchers = 1;

    #endregion

    #region Blind action

    /// <summary>
    /// Время, через которое начнется ослепление после активации способности
    /// </summary>
    [DataField]
    public TimeSpan StartBlindTime = TimeSpan.FromSeconds(12f);

    /// <summary>
    /// Время ослепления после успешного применения способности
    /// </summary>
    [DataField]
    public TimeSpan BlindnessTime = TimeSpan.FromSeconds(7);

    #endregion

    /// <summary>
    /// Звук, издающийся при прыжке
    /// </summary>
    [DataField]
    public SoundSpecifier TeleportationSound = new SoundCollectionSpecifier("FootstepScp173Classic");

    /// <summary>
    /// Количество жидкости вокруг сущности, рассчитывается для виджета заполненности камеры
    /// </summary>
    [AutoNetworkedField]
    public FixedPoint2 ReagentVolumeAround;

    /// <summary>
    /// Прототип реагента, который создает объект при засорении.
    /// </summary>
    [ViewVariables]
    public static readonly ProtoId<ReagentPrototype> Reagent = "Scp173Reagent";

    /// <summary>
    /// Количество реагента, которое необходимо накопить вокруг, засорение открывало шлюзы вокруг.
    /// </summary>
    [ViewVariables]
    public const int MinTotalSolutionVolume = 600;

    /// <summary>
    /// Количество реагента, которое необходимо накопить вокруг, чтобы начать взрываться при засорении
    /// </summary>
    [ViewVariables]
    public const int ExtraMinTotalSolutionVolume = 900;
}
