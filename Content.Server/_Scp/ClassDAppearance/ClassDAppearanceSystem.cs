using Content.Server.Antag;
using Content.Shared._Scp.ClassDAppearance;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Scp.ClassDAppearance;

public sealed class ClassDAppearanceSystem : SharedClassDAppearanceSystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;



    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClassDAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ClassDAppearanceComponent> ent, ref MapInitEvent args)
    {

        var name = "D-" + _random.Next(1000, 9999);

        _metaData.SetEntityName(ent, name);
    }
}
