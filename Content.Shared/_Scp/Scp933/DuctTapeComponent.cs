using System;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Компонент для ленты SCP-933.
/// Использует стандартный Stack компонент для количества использований.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DuctTapeComponent : Component
{
    public const float MinimumPeelDelaySeconds = 0.1f;

    private float _peelDelaySeconds = 3.5f;

    /// <summary>
    /// Прототип полоски ленты, которая отрывается от рулона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId TapeMaskPrototype = "ClothingMaskScp933Tape";

    /// <summary>
    /// Время отрыва одной полоски от рулона.
    /// Минимальное значение ограничено MinimumPeelDelaySeconds.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PeelDelaySeconds
    {
        get => _peelDelaySeconds;
        set => _peelDelaySeconds = MathF.Max(MinimumPeelDelaySeconds, value);
    }

    /// <summary>
    /// Звук, когда отрывают полоску от рулона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier PullFromRollSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");

    /// <summary>
    /// Прерывать ли do-after при движении.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnMove = true;

    /// <summary>
    /// Прерывать ли do-after при получении урона.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnDamage = true;

    /// <summary>
    /// Прерывать ли do-after при выбрасывании предмета.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnDropItem = true;

    /// <summary>
    /// Прерывать ли do-after при смене руки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BreakOnHandChange = true;

    /// <summary>
    /// Требуется ли свободная рука.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedHand = true;
}
