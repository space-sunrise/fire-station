using Content.Shared._Scp.Other.ClassDAppearance;
using Robust.Shared.Random;

namespace Content.Server._Scp.ClassDAppearance;

public sealed class ClassDAppearanceSystem : EntitySystem
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
