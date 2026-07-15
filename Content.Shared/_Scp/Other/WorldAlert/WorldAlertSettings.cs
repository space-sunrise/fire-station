using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.WorldAlert;

[DataDefinition, Serializable, NetSerializable]
public partial record struct WorldAlertSettings
{
    [DataField]
    public EntProtoId? Prototype;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public bool DirectSound;

    [DataField]
    public TimeSpan? Lifetime;
}
