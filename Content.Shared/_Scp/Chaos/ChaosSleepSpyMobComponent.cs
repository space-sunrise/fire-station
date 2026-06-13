using Content.Shared.Objectives.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Chaos;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChaosSleepSpyMobComponent : Component
{
    [DataField]
    public Color CodeWordColor = Color.FromHex("#cc3b3b");

    [DataField]
    public EntProtoId<ObjectiveComponent> HelpObjectiveProtoId = "";

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/The_Chaos_Insurgency_Theme.ogg");

    [ViewVariables]
    public string[]? CodeWords;
}