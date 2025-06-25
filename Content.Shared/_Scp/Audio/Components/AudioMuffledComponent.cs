using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Audio.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AudioMuffledComponent : Component
{
    [ViewVariables] public float CachedVolume;
    [ViewVariables] public ProtoId<AudioPresetPrototype>? CachedPreset;
}
