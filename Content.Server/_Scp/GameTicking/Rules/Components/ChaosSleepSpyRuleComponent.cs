using Robust.Shared.Audio;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosSpyRuleSystem), typeof(ChaosSleepSpyRuleSystem))]
public sealed partial class ChaosSleepSpyRuleComponent : Component
{
    [ViewVariables]
    public string[]? CodeWords;
}
