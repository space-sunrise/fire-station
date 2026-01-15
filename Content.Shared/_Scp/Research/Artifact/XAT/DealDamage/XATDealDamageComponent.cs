using Content.Shared.Damage;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Research.Artifact.XAT.DealDamage;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XATDealDamageComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier RequiredDamage;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [ViewVariables, AutoNetworkedField]
    public DamageSpecifier AccumulatedDamage = new();
}
