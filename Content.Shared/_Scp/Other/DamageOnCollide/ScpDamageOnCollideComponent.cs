using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.DamageOnCollide;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpDamageOnCollideComponent : Component
{
    [DataField(required: true)]
    public List<DamageOnCollideParameters> Params = [];
}

[DataDefinition, Serializable]
public partial struct DamageOnCollideParameters()
{
    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    [DataField]
    public SoundSpecifier? TargetSound;

    [DataField]
    public SoundSpecifier? EntitySound;

    [DataField]
    public List<MobState>? RequiredMobStates;

    [DataField]
    public bool RequiresVelocity;

    [DataField]
    public bool UseVariance;
}
