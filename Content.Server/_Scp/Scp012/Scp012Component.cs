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

    [DataField(required: true)]
    public DamageSpecifier PassiveDamage;

    [DataField(required: true)]
    public DamageSpecifier SuicideDamage;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;
}
