using Content.Shared.Random.Rules;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.SpawnInEntityStorage;

[RegisterComponent]
public sealed partial class SpawnInEntityStorageRuleComponent : Component
{
    [DataField(required: true)]
    public float Probability;

    [DataField(required: true)]
    public List<EntitySpawnEntry> Entities = default!;

    [DataField]
    public bool DoOpenCloseAnimation = true;

    [DataField]
    public TimeSpan CloseAfter = TimeSpan.FromSeconds(1f);

    [DataField]
    public TimeSpan CloseAfterVariation = TimeSpan.FromSeconds(0.5f);

    [DataField]
    public ProtoId<RulesPrototype>? StationRules;
}
