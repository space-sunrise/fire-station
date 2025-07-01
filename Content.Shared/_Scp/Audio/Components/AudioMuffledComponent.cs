using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Audio.Components;

/// <summary>
/// Компонент для кэширования исходных параметров звука при применении эффекта приглушения.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class AudioMuffledComponent : Component
{
    /// <summary>
    /// Кэшированная громкость звука до применения эффекта приглушения.
    /// </summary>
    [ViewVariables] public float CachedVolume;

    /// <summary>
    /// Кэшированный пресет звука до применения эффекта приглушения.
    /// </summary>
    [ViewVariables] public ProtoId<AudioPresetPrototype>? CachedPreset;
}
