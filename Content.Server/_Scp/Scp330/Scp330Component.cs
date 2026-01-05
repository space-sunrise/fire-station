namespace Content.Server.Scp330;

[RegisterComponent]
public sealed partial class Scp330Component : Component
{
    [DataField("regenDelay")]
    public float RegenDelay = 10f;

    public float Accumulator = 0f;

    [DataField("maxCandies")]
    public int MaxCandies = 10;
    
    // количество конфет в миске
    public int CurrentCandies = 10;

    [DataField("baseDamage")]
    public float BaseDamage = 20f;

    public Dictionary<EntityUid, int> ThiefCounter = new();
}