using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp173;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp173Component : Component
{
    [DataField, ViewVariables]
    public float WatchRange = 12f;

    #region Fast movement action

    [DataField, ViewVariables]
    public float MaxJumpRange = 4f;

    [DataField, ViewVariables]
    public int MaxWatchers = 1;

    #endregion

    [DataField]
    public SoundSpecifier NeckSnapSound = new SoundCollectionSpecifier("Scp173NeckSnap");

    [DataField]
    public SoundSpecifier TeleportationSound = new SoundCollectionSpecifier("FootstepScp173Classic");

    [DataField, ViewVariables]
    public DamageSpecifier? NeckSnapDamage;

}
