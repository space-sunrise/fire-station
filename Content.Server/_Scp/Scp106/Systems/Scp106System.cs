using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.Gateway.Systems;
using Content.Server.Ghost;
using Content.Server.Jittering;
using Content.Server.Speech.EntitySystems;
using Content.Server.Station.Components;
using Content.Server.Store.Systems;
using Content.Server.Stunnable;
using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Systems;
using Content.Shared.Alert;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Helpers;
using Content.Shared.StatusEffect;
using Content.Shared.Store.Components;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106System : SharedScp106System
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StairsSystem _stairs = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;
    [Dependency] private readonly StutteringSystem _stuttering = default!;

    private readonly SoundSpecifier _sendBackroomsSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/onbackrooms.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent((Entity<Scp106BackRoomMapComponent> _, ref AttemptGatewayOpenEvent args) => args.Cancelled = true);

        SubscribeLocalEvent<Scp106PhantomComponent, MobStateChangedEvent>(OnMobStateChangedEvent);

        // Store
        SubscribeLocalEvent<Scp106Component, Scp106ShopAction>(OnShop);

        // Abilities in that store
        SubscribeLocalEvent<Scp106Component, Scp106BoughtPhantomAction>(OnBoughtPhantom);
        SubscribeLocalEvent<Scp106Component, Scp106UpgradePhantomAction>(OnUpgradePhantom);
    }

    private void OnUpgradePhantom(EntityUid uid,
        Scp106Component component,
        Scp106UpgradePhantomAction args)
    {
        // logic
    }
    public override bool PhantomTeleport(Scp106BecomeTeleportPhantomActionEvent args)
    {
        if (args.Args.EventTarget == null)
            return false;

        var phantom = args.Args.EventTarget.Value;

        if (!TryComp<Scp106PhantomComponent>(phantom, out var phantomComponent))
            return false;

        var scp106 = phantomComponent.Scp106BodyUid;

        var phantomPos = Transform(phantom).Coordinates;

        if (_mindSystem.TryGetMind(phantom, out var mindId, out var _))
        {
            if (!TryComp<Scp106PhantomComponent>(phantom, out var _))
                return false;

            _mindSystem.TransferTo(mindId, scp106);

            if (!TryComp<Scp106Component>(scp106, out var scp106Component))
                return false;

            scp106Component.Essence -= 10;

            Dirty(scp106, scp106Component);

            _appearanceSystem.SetData(scp106, Scp106Visuals.Visuals, Scp106VisualsState.Default);

            _transform.SetCoordinates(scp106, phantomPos);

            EntityManager.DeleteEntity(phantom);
        }

        return true;
    }

    private void OnBoughtPhantom(EntityUid uid, Scp106Component component, Scp106BoughtPhantomAction args)
    {
        component.AmoutOfPhantoms += 1;

        Dirty(uid, component);
    }

    private void OnShop(EntityUid uid, Scp106Component component, Scp106ShopAction args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    private void OnMobStateChangedEvent(EntityUid uid, Scp106PhantomComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            if (_mindSystem.TryGetMind(uid, out var mindId, out var _))
            {
                _mindSystem.TransferTo(mindId, component.Scp106BodyUid);
                EntityManager.DeleteEntity(uid);
                Dirty(uid, component);
            }
        }
    }

    private void OnMapInit(Entity<Scp106Component> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Scp106EssenceAlert);

        var marks = SearchForMarks();
        if (marks.Count == 0)
            _ = _stairs.GenerateFloor();
    }

    public override async void SendToBackrooms(EntityUid target)
    {
        // You already here.
        if (HasComp<Scp106BackRoomMapComponent>(Transform(target).MapUid))
            return;

        if (HasComp<Scp106Component>(target))
        {
            var a = await GetTransferMark();
            _transform.SetCoordinates(target, a);
            _transform.AttachToGridOrMap(target);

            return;
        }

        // Телепортировать только людей
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        var mark = await GetTransferMark();
        _transform.SetCoordinates(target, mark);
        _transform.AttachToGridOrMap(target);

        _audio.PlayEntity(_sendBackroomsSound, target, target);
    }

    public override void SendToStation(EntityUid target)
    {
        if (!TryFindRandomTile(out _, out _, out _, out var targetCoords))
            return;

        _transform.SetCoordinates(target, targetCoords);
        _transform.AttachToGridOrMap(target);
    }

    private async Task<EntityCoordinates> GetTransferMark()
    {
        var marks = SearchForMarks();
        if (marks.Count != 0)
            return _random.Pick(marks);

        // Impossible, but just to be sure.
        await _stairs.GenerateFloor();
        return _random.Pick(SearchForMarks());
    }

    private HashSet<EntityCoordinates> SearchForMarks()
    {
        return EntityQuery<Scp106BackRoomMarkComponent>()
            .Select(entity => Transform(entity.Owner).Coordinates)
            .ToHashSet();
    }

    private bool TryGetRandomStation([NotNullWhen(true)] out EntityUid? station, Func<EntityUid, bool>? filter = null)
    {
        var stations = new ValueList<EntityUid>(Count<StationEventEligibleComponent>());

        filter ??= _ => true;
        var query = AllEntityQuery<StationEventEligibleComponent>();

        while (query.MoveNext(out var uid, out _))
        {
            if (!filter(uid))
                continue;

            stations.Add(uid);
        }

        if (stations.Count == 0)
        {
            station = null;
            return false;
        }

        station = stations[_random.Next(stations.Count)];
        return true;
    }

    public bool TryFindRandomTile(out Vector2i tile,
        [NotNullWhen(true)] out EntityUid? targetStation,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetStation = EntityUid.Invalid;
        targetGrid = EntityUid.Invalid;
        targetCoords = EntityCoordinates.Invalid;
        if (TryGetRandomStation(out targetStation))
        {
            return TryFindRandomTileOnStation((targetStation.Value, Comp<StationDataComponent>(targetStation.Value)),
                out tile,
                out targetGrid,
                out targetCoords);
        }

        return false;
    }

    public bool TryFindRandomTileOnStation(Entity<StationDataComponent> station,
        out Vector2i tile,
        out EntityUid targetGrid,
        out EntityCoordinates targetCoords)
    {
        tile = default;
        targetCoords = EntityCoordinates.Invalid;
        targetGrid = EntityUid.Invalid;

        var weights = new Dictionary<Entity<MapGridComponent>, float>();
        foreach (var possibleTarget in station.Comp.Grids)
        {
            if (!TryComp<MapGridComponent>(possibleTarget, out var comp))
                continue;

            weights.Add((possibleTarget, comp), _map.GetAllTiles(possibleTarget, comp).Count());
        }

        if (weights.Count == 0)
        {
            targetGrid = EntityUid.Invalid;
            return false;
        }

        (targetGrid, var gridComp) = _random.Pick(weights);

        var found = false;
        var aabb = gridComp.LocalAABB;

        for (var i = 0; i < 10; i++)
        {
            var randomX = _random.Next((int) aabb.Left, (int) aabb.Right);
            var randomY = _random.Next((int) aabb.Bottom, (int) aabb.Top);

            tile = new Vector2i(randomX, randomY);
            if (_atmosphere.IsTileSpace(targetGrid, Transform(targetGrid).MapUid, tile)
                || _atmosphere.IsTileAirBlocked(targetGrid, tile, mapGridComp: gridComp)
                || !_map.TryGetTileRef(targetGrid, gridComp, tile, out var tileRef)
                || tileRef.Tile.IsEmpty)
            {
                continue;
            }

            found = true;
            targetCoords = _map.GridTileToLocal(targetGrid, gridComp, tile);
            break;
        }

        return found;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var queryScp106 = AllEntityQuery<Scp106Component>();

        while(queryScp106.MoveNext(out var scp106Uid, out var scp106Component))
        {
            scp106Component.Accumulator += frameTime;

            if (scp106Component.Accumulator < 1)
                continue;

            scp106Component.BackroomsAccumulator += 1;

            scp106Component.Accumulator -= 1;
            scp106Component.Essence += 1;
            scp106Component.HumansInBackrooms = 0;


            if (scp106Component.BackroomsAccumulator < 60)
            {
                Dirty(scp106Uid, scp106Component);
                continue;
            }

            scp106Component.BackroomsAccumulator = 0;

            var queryHumans = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();

            while (queryHumans.MoveNext(out var humanUid, out var _, out var mobStateComponent))
            {
                if (HasComp<Scp106BackRoomMapComponent>(Transform(humanUid).MapUid)
                    && mobStateComponent.CurrentState == MobState.Alive)
                {
                    scp106Component.HumansInBackrooms += 1;

                    if (scp106Component.HumansInBackrooms >= 10)
                    {
                        // Тут должна быть логика события, например добавление особой абилки
                        Dirty(scp106Uid, scp106Component);
                    }
                }
            }

            if (scp106Component.AnnouncementAccumulator < 600)
            {
                scp106Component.AnnouncementAccumulator += 60;
                Dirty(scp106Uid, scp106Component);
                continue;
            }

            if (scp106Component.HumansInBackrooms >= 10)
            {
                scp106Component.AnnouncementAccumulator = 0;

                var boo = new GhostBooEvent();
                RaiseLocalEvent(scp106Uid, boo, true);

                var statusEffectQuery = EntityQueryEnumerator<StatusEffectsComponent>();
                while (statusEffectQuery.MoveNext(out var ent, out var _))
                {
                    _stunSystem.TryParalyze(ent, TimeSpan.FromSeconds(5), true);
                    _jittering.DoJitter(ent, TimeSpan.FromSeconds(15), true);
                    _stuttering.DoStutter(ent, TimeSpan.FromSeconds(30), true);
                }

                _chatSystem.DispatchGlobalAnnouncement(Loc.GetString("scp-10-humans-in-backrooms-alarm-announcement"),
                    null,
                    true,
                    null,
                    true,
                    "CBMTF1",
                    Color.Red
                );

                _audio.PlayGlobal(
                    "/Audio/_Sunrise/stab.ogg",
                    Filter.Broadcast(),
                    false,
                    new AudioParams().WithVolume(-10));
            }

            Dirty(scp106Uid, scp106Component);
        }
    }
}
