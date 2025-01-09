using Robust.Shared.Audio;

namespace Content.Server._Scp.ClassDAppearance;

[RegisterComponent]
public sealed partial class ClassDAppearanceComponent : Component
{
    [DataField]
    public SoundSpecifier ClassDSpawnSound = new SoundPathSpecifier("/Audio/_Scp/class_d_spawn_sound.ogg");
}
