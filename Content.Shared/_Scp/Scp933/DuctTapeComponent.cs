using System;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Компонент для ленты SCP-933.
/// Хранит количество использований.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class DuctTapeComponent : Component
{
    public const float MinimumPeelDelaySeconds = 0.1f;

    /// <summary>
    /// Прототип полоски ленты, которая отрывается от рулона.
    /// </summary>
    [DataField]
    public EntProtoId TapeMaskPrototype = "ClothingMaskScp933Tape";

    /// <summary>
    /// Количество использований.
    /// Значение меньше нуля означает бесконечное использование.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int UseCount = -1;

    /// <summary>
    /// Время отрыва одной полоски от рулона.
    /// </summary>
    [DataField]
    public float PeelDelaySeconds = 3.5f;

    /// <summary>
    /// Безопасное время do-after с нижней границей для сервера.
    /// </summary>
    public float ValidatedPeelDelaySeconds => MathF.Max(MinimumPeelDelaySeconds, PeelDelaySeconds);

    /// <summary>
    /// Звук, когда отрывают полоску от рулона.
    /// </summary>
    [DataField]
    public SoundSpecifier PullFromRollSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");
}
