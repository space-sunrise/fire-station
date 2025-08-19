using Content.Shared.Radio;
using Robust.Shared.Audio;
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

    [DataField]
    public SoundSpecifier SendSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Radio/send.ogg", AudioParams.Default.AddVolume(-7));

    [DataField]
    public SoundSpecifier ReceiveSound = new SoundCollectionSpecifier("RadioReceive", AudioParams.Default.AddVolume(-7).WithMaxDistance(2f));

    [DataField]
    public SoundSpecifier ChannelCycleSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Radio/cycle.ogg", AudioParams.Default.AddVolume(-7));

    [DataField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Radio/toggle.ogg", AudioParams.Default.AddVolume(-7));
}
