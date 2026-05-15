using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(ChaosRaidRuleSystem))]
public sealed partial class ChaosRaidRuleComponent : Component
{
    [ViewVariables]
    public int RoundstartRaidersCount = 0;

    [DataField]
    public EntityUid? TargetComplex;

    [DataField]
    public ProtoId<NpcFactionPrototype> Faction = "Chaos";

    [DataField]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/_Scp/Themes/The_Chaos_Insurgency_Theme.ogg");
}
