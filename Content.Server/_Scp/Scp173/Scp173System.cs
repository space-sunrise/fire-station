using System.Linq;
using System.Numerics;
using Content.Server.Ghost;
using Content.Server.Light.Components;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Scp173;
using Content.Shared.Damage;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Scp173;

public sealed class Scp173System : EntitySystem
{
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, Scp173DamageStructureAction>(OnStructureDamage);
    }

    private void OnStructureDamage(Entity<Scp173Component> uid, ref Scp173DamageStructureAction args)
    {
        if (args.Handled)
            return;

        var defileRadius = 3f;
        var defileTilePryAmount = 10;

        var xform = Transform(uid);
        if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
            return;
        var tiles = map.GetTilesIntersecting(Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
            new Vector2(defileRadius * 2, defileRadius))).ToArray();

        _random.Shuffle(tiles);

        for (var i = 0; i < defileTilePryAmount; i++)
        {
            if (!tiles.TryGetValue(i, out var value))
                continue;

            _tile.PryTile(value);
        }

        var lookup = _lookup.GetEntitiesInRange(uid, defileRadius, LookupFlags.Approximate | LookupFlags.Static);
        var tags = GetEntityQuery<TagComponent>();
        var entityStorage = GetEntityQuery<EntityStorageComponent>();
        var items = GetEntityQuery<ItemComponent>();
        var lights = GetEntityQuery<PoweredLightComponent>();

        foreach (var ent in lookup)
        {
            // break windows/walls
            if (tags.HasComponent(ent))
            {
                if (_tag.HasTag(ent, "Window") || _tag.HasTag(ent, "Wall"))
                {
                    var dspec = new DamageSpecifier();
                    dspec.DamageDict.Add("Structural", 20);
                    _damageable.TryChangeDamage(ent, dspec, true);
                }
            }

            // randomly opens some lockers and such.
            if (entityStorage.TryGetComponent(ent, out var entstorecomp))
                _entityStorage.OpenStorage(ent, entstorecomp);

            // chucks items
            if (items.HasComponent(ent) &&
                TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

            // flicker lights
            if (lights.HasComponent(ent))
                _ghost.DoGhostBooEvent(ent);
        }

        // TODO: Sound.

        args.Handled = true;
    }
}
