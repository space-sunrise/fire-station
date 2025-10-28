using Robust.Shared.Audio;

namespace Content.Server._Scp.Other.BreakDoorOnCollide;

[RegisterComponent]
public sealed partial class BreakDoorOnCollideComponent : Component
{
    [DataField]
    public float WireCutChance = 0.4f;

    [DataField]
    public SoundSpecifier DoorSmashSoundCollection =
        new SoundCollectionSpecifier("MetalSlam", AudioParams.Default.WithVolume(-2f).WithVariation(0.2f));
}
