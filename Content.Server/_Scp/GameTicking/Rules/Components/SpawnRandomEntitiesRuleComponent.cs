using Content.Shared.Storage;

namespace Content.Server._Scp.GameTicking.Rules.Components;

/// <summary>
/// This handles starting various roundstart variation rules after a station has been loaded.
/// </summary>
[RegisterComponent]
public sealed partial class SpawnRandomEntitiesRuleComponent : Component
{
    /// <summary>
    ///     Number of tiles before we spawn one entity on average.
    /// </summary>
    [DataField]
    public float TilesPerEntityAverage = 50f;

    [DataField]
    public float TilesPerEntityStdDev = 7f;

    /// <summary>
    ///     Spawn entries for each chosen location.
    /// </summary>
    [DataField(required: true)]
    public List<EntitySpawnEntry> Entities = default!;

}
