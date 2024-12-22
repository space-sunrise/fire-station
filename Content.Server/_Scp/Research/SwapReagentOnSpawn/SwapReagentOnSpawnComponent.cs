namespace Content.Server._Scp.Research.SwapReagentOnSpawn;

[RegisterComponent]
public sealed partial class SwapReagentOnSpawnComponent : Component
{
    [DataField(required: true)]
    public Dictionary<string, string> Replace = new ();

    /// <summary>
    /// Шанс замены реагента с заменяемого на заменяющий
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Chance
    {
        get => _chance;
        set => _chance = Math.Clamp(value, 0f, 1f);
    }

    private float _chance = 1f;
}
