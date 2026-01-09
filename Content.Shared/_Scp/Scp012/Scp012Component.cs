using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp012;

[RegisterComponent, NetworkedComponent]
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
