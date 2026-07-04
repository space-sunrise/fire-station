using Content.Shared.Objectives.Components;
using Content.Shared.Random;
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
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/The_Chaos_Insurgency_Theme.ogg");

    [DataField]
    public EntProtoId DefaultChaosSpyRule = "ScpChaosLowSpy";

    [ViewVariables]
    public string[]? CodeWords;
}
