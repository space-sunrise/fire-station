using Content.Server.GameTicking;
using Content.Shared.Trigger.Systems;

namespace Content.Server._Scp.Trigger.ScpTriggerOnSpawn;

[RegisterComponent]
public sealed partial class ScpTriggerOnSpawnComponent : Component
{
    [DataField]
    public string? KeyOut = TriggerSystem.DefaultTriggerKey;

    [DataField]
    public GameRunLevel? RequiredGameRunLevel;
}
