using Content.Shared.Random.Rules;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.SpawnInPlayerInventory;

[RegisterComponent]
public sealed partial class SpawnInPlayerInventoryRuleComponent : Component
{
    [DataField(required: true)]
    public float Probability;

    [DataField(required: true)]
    public List<EntitySpawnEntry> Entities = default!;

    [DataField]
    public SoundSpecifier? Sound;

    [DataField]
    public ProtoId<RulesPrototype>? StationRules;
}
