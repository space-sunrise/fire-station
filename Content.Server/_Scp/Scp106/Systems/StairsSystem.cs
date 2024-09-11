using System.Threading.Tasks;
using Content.Server._Scp.Scp106.Components;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Procedural;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;
using Content.Shared.Procedural;
using Content.Shared.Tag;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class StairsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StaircaseComponent, StartCollideEvent>(OnCollideInit);
        SubscribeLocalEvent<StaircaseComponent, InteractHandEvent>(OnInteract);
    }

    private void OnCollideInit(EntityUid uid, StaircaseComponent component, ref StartCollideEvent args)
    {
        if (component.Generating)
            return;

        if (!HasComp<ActorComponent>(args.OtherEntity) || !HasComp<MobStateComponent>(args.OtherEntity))
            return;

        if (component.LinkedStair != null)
            return;

        component.Generating = true;
        _ = GenerateFloorWithStairLinking(uid, component);
    }

    private void OnInteract(EntityUid uid, StaircaseComponent component, InteractHandEvent args)
    {
        if (component.LinkedStair is not { } stair)
            return;

        var xform = Transform(stair);
        _transform.SetCoordinates(args.User, xform.Coordinates);
    }

    public async Task<MapId> GenerateFloor()
    {
        var map = _mapSystem.CreateMap(out var mapId);

        var gravity = EnsureComp<GravityComponent>(map);
        gravity.Enabled = true;
        Dirty(map, gravity);

        var light = EnsureComp<MapLightComponent>(map);
        light.AmbientLightColor = Color.FromHex("#181624");
        Dirty(map, light);

        EnsureComp<MapAtmosphereComponent>(map);
        EnsureComp<Scp106BackRoomMapComponent>(map);

        _atmosphere.SetMapSpace(map, false);

        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 21.824779f;
        moles[(int) Gas.Nitrogen] = 82.10312f;

        var mixture = new GasMixture(moles, Atmospherics.T20C);

        _atmosphere.SetMapAtmosphere(map, false, mixture);

        var gridComp = EnsureComp<MapGridComponent>(map);

        await _dungeon.GenerateDungeonAsync(_prototype.Index<DungeonConfigPrototype>("Backrooms"), map, gridComp, Vector2i.Zero, _random.Next());

        return mapId;
    }

    public async Task GenerateFloorWithStairLinking(EntityUid uid, StaircaseComponent component)
    {
        var map = await GenerateFloor();

        if (FindValidStairOnFloor(map) is not { } pair)
            return;

        var secondStairEnt = pair.Item1;
        var secondStairComp = pair.Item2;
        component.LinkedStair = secondStairEnt;
        secondStairComp.LinkedStair = uid;

        component.Generating = false;
    }

    private (EntityUid, StaircaseComponent)? FindValidStairOnFloor(MapId mapId)
    {
        var query = EntityQueryEnumerator<StaircaseComponent, TagComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stair, out var tag, out var xform))
        {
            if (stair.LinkedStair != null)
                continue;

            if (xform.MapID != mapId)
                continue;

            if (!_tag.HasTag(tag, "UpStairs106"))
                continue;

            return (uid, stair);
        }

        return null;
    }
}
