using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Misc.SwapEntityOnSpawn;

[RegisterComponent]
public sealed partial class SwapEntityOnSpawnComponent : Component
{
    [DataField(required: true)]
    public HashSet<EntProtoId> Replace = new ();

    /// <summary>
    /// Шанс замены ентити с заменяемого на заменяющий
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Chance
    {
        get => _chance;
        set => _chance = Math.Clamp(value, 0f, 1f);
    }

    private float _chance = 1f;
}
