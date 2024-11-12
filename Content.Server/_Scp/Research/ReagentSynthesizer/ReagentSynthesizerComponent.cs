using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Audio;

namespace Content.Server._Scp.Research.ReagentSynthesizer;

[RegisterComponent]
public sealed partial class ReagentSynthesizerComponent : Component
{
    [DataField(required: true)]
    public HashSet<ReagentId> Reagents = new();

    [DataField]
    public TimeSpan WorkTime = TimeSpan.FromSeconds(60);

    #region Sounds

    [DataField]
    public SoundSpecifier ClickSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier ActiveSound = new SoundPathSpecifier("/Audio/Machines/blender.ogg");

    #endregion

    public EntityUid? AudioStream;
}

[RegisterComponent]
public sealed partial class ActiveReagentSynthesizerComponent : Component
{
    [ViewVariables]
    public TimeSpan EndTime;

    [ViewVariables]
    public TimeSpan TimeWithoutEnergy;
}
