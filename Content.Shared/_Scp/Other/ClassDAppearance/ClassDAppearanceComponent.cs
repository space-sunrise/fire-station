using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Other.ClassDAppearance;

[RegisterComponent, NetworkedComponent]
public sealed partial class ClassDAppearanceComponent : Component
{
    [DataField]
    public SoundSpecifier ClassDSpawnSound = new SoundPathSpecifier("/Audio/_Scp/class_d_spawn_sound.ogg");
}
