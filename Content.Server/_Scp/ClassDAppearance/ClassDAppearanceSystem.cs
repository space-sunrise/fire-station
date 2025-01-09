using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Scp.ClassDAppearance;

public sealed class ClassDAppearanceSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClassDAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ClassDAppearanceComponent> ent, ref MapInitEvent args)
    {

        _sharedAudioSystem.PlayEntity(ent.Comp.ClassDSpawnSound, ent, ent);


        var name = "D-" + _random.Next(1000, 9999);

        _metaData.SetEntityName(ent, name);
    }
}
