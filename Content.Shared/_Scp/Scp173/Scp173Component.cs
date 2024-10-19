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

    [DataField, ViewVariables]
    public SoundSpecifier NeckSnapSound = new SoundCollectionSpecifier("Scp173NeckSnap");

    [DataField, ViewVariables]
    public DamageSpecifier? NeckSnapDamage;

    #region Research

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ChipOffDelay = 30f;

    [DataField, ViewVariables]
    public SoundSpecifier ChipOffSound = new SoundPathSpecifier("/Audio/Effects/break_stone.ogg");

    [DataField, ViewVariables]
    public TimeSpan ChipOffCooldown = TimeSpan.FromSeconds(600f); // 10 минут

    [DataField, ViewVariables]
    public TimeSpan? ChipOffLastUsed;

    #endregion


}
