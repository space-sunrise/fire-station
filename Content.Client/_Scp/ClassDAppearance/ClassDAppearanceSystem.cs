using Content.Shared._Scp.ClassDAppearance;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Client._Scp.ClassDAppearance;

public sealed class ClassDAppearanceSystem : SharedClassDAppearanceSystem
{
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClassDAppearanceComponent, ComponentInit>(OnInit);
    }

    private void OnInit(EntityUid uid, ClassDAppearanceComponent component, ComponentInit args)
    {
        _sharedAudioSystem.PlayGlobal("/Audio/_Scp/class_d_spawn_sound.ogg", Filter.Local(), false, AudioParams.Default);
    }
}
