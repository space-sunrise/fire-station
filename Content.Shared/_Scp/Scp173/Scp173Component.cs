using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp173;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp173Component : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float WatchRange = 12f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxJumpRange = 8f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier NeckSnapSound = new SoundCollectionSpecifier("Scp173NeckSnap");

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier? NeckSnapDamage;
}
