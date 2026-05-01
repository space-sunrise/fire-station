using Robust.Shared.Audio;

namespace Content.Shared._Scp.Knowledge.Components;

[RegisterComponent]
public sealed partial class ScpKnowledgeAcquisitionComponent : Component
{
    [DataField]
    public bool CanLearnByListen = true;

    [DataField]
    public bool CanLearnByRead = true;

    [DataField]
    public bool CanLearnByExamine = true;

    [DataField]
    public bool CanLearnByOther = true;

    [DataField]
    public SoundSpecifier? UnlockSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg", AudioParams.Default.WithVolume(-6f));
}
