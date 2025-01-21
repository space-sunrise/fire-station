using Content.Shared._Scp.Other.ClassDAppearance;
using Robust.Server.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Scp.ClassDAppearance;

public sealed class ClassDAppearanceSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ClassDAppearanceComponent, MapInitEvent>(OnMapInit);

        // Прости сплик, но я 2 раза заспавнился за класс д на локалке и меня этот звук уже заебал
        // SubscribeLocalEvent<ClassDAppearanceComponent, PlayerAttachedEvent>(OnPlayerAttachedEvent);
    }

    private void OnPlayerAttachedEvent(Entity<ClassDAppearanceComponent> ent, ref PlayerAttachedEvent args)
    {
        _audio.PlayEntity(ent.Comp.ClassDSpawnSound, ent, ent);
    }

    private void OnMapInit(Entity<ClassDAppearanceComponent> ent, ref MapInitEvent args)
    {
        var name = "D-" + _random.Next(1000, 9999);

        _metaData.SetEntityName(ent, name);
    }
}
