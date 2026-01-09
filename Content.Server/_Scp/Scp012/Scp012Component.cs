using Content.Shared.Damage;
using Content.Shared.Whitelist;

namespace Content.Server._Scp.Scp012;

[RegisterComponent]
public sealed partial class Scp012Component : Component
{
    [DataField]
    public float Range = 7f;

    [DataField]
    public float AttractionForce = 1.5f;

    [DataField]
    public float SuicideTimer = 30f;

    [DataField(required: true)]
    public DamageSpecifier Damage;

    [DataField]
    public TimeSpan DamageCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan? NextDamageTime;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
