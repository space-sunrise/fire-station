using Content.Shared._Scp.GameRule.Sl;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameRules.ScpSl;

[RegisterComponent]
public sealed partial class ScpSlGameRuleComponent : Component
{

    [DataField(required: true)]
    public Dictionary<ScpSlHumanoidType, List<ProtoId<RandomHumanoidSettingsPrototype>>> HumanoidPresets { get; private set; } = new();

    public Dictionary<SlZoneType, Entity<MapGridComponent>> Zones { get; private set; } = new();

    [DataField(required: true)]
    public ProtoId<RandomHumanoidSettingsPrototype> MogCaptainPrototype { get; set; }

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan WaveSpawnCooldown { get; set; } = TimeSpan.FromSeconds(300);

    public TimeSpan NextWaveSpawnTime { get; set; }

    public int EscapedScientists = 0;
    public int EscapedDClass = 0;

    public int ContainedScps = 0;

    public SlWinType WinType = SlWinType.Tie;
}

public enum SlWinType : byte
{
    FondWin = 0,
    Tie = 1,
    ScpWin = 2,
    ChaosWin = 3
}

public enum SlZoneType
{
    Light,
    Hard,
    Office
}
