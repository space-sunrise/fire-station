using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Other.Radio;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpRadioComponent : Component
{
    [DataField(required: true)]
    public List<ProtoId<RadioChannelPrototype>> Channels;

    [DataField]
    public ProtoId<RadioChannelPrototype> ActiveChannel;

    [DataField]
    public bool SpeakerEnabled = true;

    [DataField]
    public bool MicrophoneEnabled;

    [DataField]
    public float ListenRange = 1f;
}
