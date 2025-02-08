using System.Linq;
using System.Threading.Tasks;
using Content.Server._Scp.Helpers;
using Content.Server._Scp.Scp106.Components;
using Content.Server.Actions;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
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
using Content.Shared.Actions;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.StatusEffect;
using Content.Shared.Store.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106System : SharedScp106System
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StairsSystem _stairs = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mindSystem = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly StunSystem _stunSystem = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;
    [Dependency] private readonly StutteringSystem _stuttering = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;


    private readonly SoundSpecifier _sendBackroomsSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/onbackrooms.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent((Entity<Scp106BackRoomMapComponent> _, ref AttemptGatewayOpenEvent args) => args.Cancelled = true);

        SubscribeLocalEvent<Scp106PhantomComponent, MobStateChangedEvent>(OnMobStateChangedEvent);

        // Store
        SubscribeLocalEvent<Scp106Component, Scp106ShopAction>(OnShop);

        // Abilities in that store - I love lambdas >:)
        SubscribeLocalEvent((EntityUid uid, Scp106Component component, Scp106BoughtPhantomAction _) =>
            _actions.AddAction(uid, component.PhantomAction));
        SubscribeLocalEvent((EntityUid _, Scp106Component component, Scp106OnUpgradePhantomAction _) =>
            component.PhantomCoolDown -= TimeSpan.FromSeconds(60));
        SubscribeLocalEvent((EntityUid uid, Scp106Component _, Scp106BoughtBareBladeAction _) =>
            _actions.AddAction(uid, "Scp106BareBlade"));

        SubscribeLocalEvent<Scp106Component, Scp106TeleporationDelayActionEvent>(OnScp106TeleporationDelayActionEvent);
    }

    public override void Scp106SpawnPortal(EntityUid uid, Scp106Component component)
    {
        component.Scp106HasPortals += 1;

        var portalUid = Spawn("Scp106PortalPortal", Transform(uid).Coordinates);
        Comp<Scp106PortalSpawnerComponent>(portalUid).Scp106 = uid;
    }

    private void OnScp106TeleporationDelayActionEvent(EntityUid uid,
        Scp106Component component,
        Scp106TeleporationDelayActionEvent args)
    {
        _appearanceSystem.SetData(uid, Scp106Visuals.Visuals, Scp106VisualsState.Default);
    }

    public override bool PhantomTeleport(Scp106BecomeTeleportPhantomActionEvent args)
    {
        if (args.Args.EventTarget is not {} phantom)
            return false;

        if (!TryComp<Scp106PhantomComponent>(phantom, out var phantomComponent))
            return false;

        var scp106 = phantomComponent.Scp106BodyUid;

        var phantomPos = Transform(phantom).Coordinates;

        if (_mindSystem.TryGetMind(phantom, out var mindId, out var _))
        {
            _mindSystem.TransferTo(mindId, scp106);

            if (!TryComp<Scp106Component>(scp106, out var scp106Component))
                return false;

            scp106Component.Essence -= 10;
            Dirty(scp106, scp106Component);

            _transform.SetCoordinates(scp106, phantomPos);

            Del(phantom);

            Scp106FinishTeleportation(scp106);
        }

        return true;
    }

    private void OnShop(EntityUid uid, Scp106Component component, Scp106ShopAction args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    private void OnMobStateChangedEvent(EntityUid uid, Scp106PhantomComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        _mindSystem.TryGetMind(uid, out var mindId, out var _);
        _mindSystem.TransferTo(mindId, component.Scp106BodyUid);
        component.Scp106BodyUid = default;
        Dirty(uid, component);
    }

    private void OnMapInit(Entity<Scp106Component> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Scp106EssenceAlert);

        var marks = SearchForMarks();
        if (marks.Count == 0)
            _ = _stairs.GenerateFloor();
    }

    public override async void SendToBackrooms(EntityUid target, EntityUid? scp106 = null)
    {
        // You already here.
        if (HasComp<Scp106BackRoomMapComponent>(Transform(target).MapUid))
            return;

        if (HasComp<Scp106Component>(target))
        {
            var a = await GetTransferMark();
            _transform.SetCoordinates(target, a);
            _transform.AttachToGridOrMap(target);
            Scp106FinishTeleportation(target);
            return;
        }

        // Телепортировать только людей
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        var mark = await GetTransferMark();
        _transform.SetCoordinates(target, mark);
        _transform.AttachToGridOrMap(target);
        _stunSystem.TryParalyze(target, TimeSpan.FromSeconds(5), true);
        _audio.PlayEntity(_sendBackroomsSound, target, target);
        if (scp106 != null)
        {
            AddCurrencyInStore(scp106.Value);
        }
    }

    public override void Scp106FinishTeleportation(EntityUid uid)
    {
        _stun.TryStun(uid, TimeSpan.FromSeconds(5), false);
        _appearanceSystem.SetData(uid, Scp106Visuals.Visuals, Scp106VisualsState.Exiting);

        var doAfterEventArgs = new DoAfterArgs(EntityManager,
            uid,
            TimeSpan.FromSeconds(5),
            new Scp106TeleporationDelayActionEvent(),
            uid)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            RequireCanInteract = false,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    public override void SendToStation(EntityUid target)
    {
        if (!_scpHelpers.TryFindRandomTile(out _, out _, out _, out var targetCoords))
            return;

        _transform.SetCoordinates(target, targetCoords);
        _transform.AttachToGridOrMap(target);
        Scp106FinishTeleportation(target);
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
            scp106Component.Essence += 1;

            scp106Component.Accumulator -= 1;
            var HumansInBackrooms = 0;

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
                    HumansInBackrooms += 1;

                    if (HumansInBackrooms >= 10)
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

            if (HumansInBackrooms >= 10)
            {
                scp106Component.AnnouncementAccumulator = 0;

                RaiseLocalEvent(scp106Uid, new GhostBooEvent(), true);

                var statusEffectQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, StatusEffectsComponent>();
                while (statusEffectQuery.MoveNext(out var ent, out var _, out var _))
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

    public void AddCurrencyInStore(EntityUid uid)
    {
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2>() { { "LifeEssence", 2f }, }, uid);
    }
}
