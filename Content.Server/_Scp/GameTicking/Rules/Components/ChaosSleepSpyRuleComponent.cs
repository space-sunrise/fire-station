using Content.Server.Antag.Components;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosSpyRuleSystem), typeof(ChaosSleepSpyRuleSystem))]
public sealed partial class ChaosSleepSpyRuleComponent : Component
{
    [DataField(required: true)]
    public AntagSelectionDefinition Definition;

    [DataField]
    public Color CodeWordColor = Color.FromHex("#cc3b3b");

    [ViewVariables]
    public string[]? CodeWords;
}