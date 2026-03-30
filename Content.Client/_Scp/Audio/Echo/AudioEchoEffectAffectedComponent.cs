using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Audio.Echo;

/// <summary>
/// Компонент-маркер, указывающий, что звук получил эффект эха.
/// </summary>
[RegisterComponent]
public sealed partial class AudioEchoEffectAffectedComponent : Component
{
    /// <summary>
    /// Пресет эффекта эха, который был использован к этому звуку.
    /// Нужен, чтобы после убрать именно его при необходимости
    /// </summary>
    [ViewVariables]
    public ProtoId<AudioPresetPrototype> Preset;
}
