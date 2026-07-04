using Robust.Shared.Audio;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosSpyRuleSystem), typeof(ChaosSleepSpyRuleSystem))]
public sealed partial class ChaosSleepSpyRuleComponent : Component
{
    [DataField]
    public Color CodeWordColor = Color.FromHex("#cc3b3b");

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/The_Chaos_Insurgency_Theme.ogg");

    [ViewVariables]
    public string[]? CodeWords;
}
