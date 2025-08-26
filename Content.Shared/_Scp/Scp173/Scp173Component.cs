using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp173;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp173Component : Component
{
    [DataField]
    public float WatchRange = 12f;

    #region Fast movement action

    [DataField]
    public float MaxJumpRange = 4f;

    [DataField]
    public int MaxWatchers = 1;

    #endregion

    [DataField]
    public SoundSpecifier NeckSnapSound = new SoundCollectionSpecifier("Scp173NeckSnap");

    [DataField]
    public SoundSpecifier TeleportationSound = new SoundCollectionSpecifier("FootstepScp173Classic");

    [DataField]
    public DamageSpecifier? NeckSnapDamage;

    [ViewVariables]
    public static readonly ProtoId<ReagentPrototype> Reagent = "Scp173Reagent";
}
