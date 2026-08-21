using Content.Server.RoundEnd;
using Content.Server._Scp.Objectives.Systems;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosRaidRuleSystem), typeof(ScpRaidHelpConditionSystem))]
public sealed partial class ChaosRaidRuleComponent : Component
{
    [DataField]
    public RoundEndBehavior RoundEndBehavior = RoundEndBehavior.ShuttleCall;

    [DataField]
    public string RoundEndTextSender = "comms-console-announcement-title-regional-administration";

    [DataField]
    public string RoundEndTextShuttleCall = "chaos-raid-no-more-threat-announcement-shuttle-call";

    [DataField]
    public string RoundEndTextAnnouncement = "chaos-raid-no-more-threat-announcement";

    [DataField]
    public TimeSpan EvacShuttleTime = TimeSpan.FromMinutes(2);

    [ViewVariables]
    public int RoundstartRaidersCount = 0;

    [ViewVariables]
    public int AliveRaidersCount = 0;

    [DataField]
    public EntityUid? TargetComplex;

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "Chaos";

    [ViewVariables]
    public int CompletedObjectivesCount = 0;

    [DataField]
    public TimeSpan ObjectivesCheckInterval = TimeSpan.FromSeconds(30);

    [ViewVariables]
    public TimeSpan NextObjectivesCheck = TimeSpan.Zero;

    [ViewVariables]
    public bool TargetEnterAnnounced;

    [ViewVariables]
    public Dictionary<EntityUid, float>? Objectives;

    [DataField]
    public ChaosWinType WinType = ChaosWinType.Neutral;

    [DataField]
    public List<ChaosWinCondition> WinConditions = new();

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/The_Chaos_Insurgency_Theme.ogg");
}

public enum ChaosWinType : byte
{
    ChaosMajor,
    ChaosMinor,
    Neutral,
    CrewMinor,
    CrewMajor
}

public enum ChaosWinCondition : byte
{

    ChaosRaidersCompleteAllObjectives,
    CrewKillAllChaosRaiders
}
