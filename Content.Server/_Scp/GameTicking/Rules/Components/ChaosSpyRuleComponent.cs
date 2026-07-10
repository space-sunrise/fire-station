using Content.Server._Scp.GameTicking.Rules;
using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server.Codewords;
using Content.Shared.FixedPoint;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Objectives.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosSpyRuleSystem))]
public sealed partial class ChaosSpyRuleComponent : Component
{
    [DataField]
    public List<EntProtoId<ChaosSpyRuleComponent>> VariantRuleProtoIds =
    [
        "ScpChaosHighSpy",
        "ScpChaosLowSpy",
    ];

    [DataField]
    public FixedPoint2 StartingBalance = 20;

    [DataField]
    public LocId Issuer = "objective-issuer-chaos";

    [DataField]
    public ProtoId<NpcFactionPrototype> FoundationFaction = "NanoTrasen";

    [DataField]
    public ProtoId<NpcFactionPrototype> ChaosFaction = "Chaos";

    [DataField]
    public ProtoId<CodewordFactionPrototype> CodewordsFactionProtoId = "ChaosSpies";

    [DataField]
    public EntProtoId<ObjectiveComponent> ChaosRaidHelpObjectiveProtoId = "ChaosSpyHelpChaosRaidObjective";

    [DataField]
    public EntProtoId<ChaosRaidRuleComponent> ChaosRaidRuleProtoId = "ScpChaosRaid";

    [DataField]
    public EntProtoId<ChaosSleepSpyRuleComponent> ChaosSleepSpyRuleProtoId = "ScpChaosSleepSpy";

    [DataField]
    public float ChaosRaidRuleChance = 0.10f;

    [DataField]
    public bool AddSleepSpies;

    [ViewVariables]
    public string[]? CodeWords;

    [DataField]
    public Color CodeWordColor = Color.Firebrick;

    [ViewVariables]
    public EntityUid? ChaosSleepSpyRuleEnt;

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/Chaos_Spy_Theme.ogg");

    [ViewVariables]
    public bool HasChaosRaidRule;
}
