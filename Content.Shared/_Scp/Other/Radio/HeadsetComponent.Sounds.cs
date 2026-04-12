#pragma warning disable IDE0130
using Robust.Shared.Audio;

namespace Content.Shared.Radio.Components;

public sealed partial class HeadsetComponent
{
    [DataField]
    public SoundSpecifier SendSound = new SoundCollectionSpecifier("HeadsetSent",
        AudioParams.Default.AddVolume(-9f).WithMaxDistance(2f));

    [DataField]
    public SoundSpecifier ReceiveSound = new SoundCollectionSpecifier("HeadsetReceive",
        AudioParams.Default.AddVolume(-7f).WithMaxDistance(2f));
}
