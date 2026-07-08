using Robust.Shared.Audio;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosSpyRuleSystem), typeof(ChaosSleepSpyRuleSystem))]
public sealed partial class ChaosSleepSpyRuleComponent : Component
{
    [DataField]
    public Color CodeWordColor = Color.Firebrick;

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/Chaos_Spy_Theme.ogg");

    [ViewVariables]
    public string[]? CodeWords;
}
