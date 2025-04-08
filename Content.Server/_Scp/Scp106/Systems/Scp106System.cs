using System.Linq;
using System.Threading.Tasks;
using Content.Server._Scp.Helpers;
using Content.Server._Scp.Scp106.Components;
using Content.Server.Actions;
using Content.Server.DoAfter;
using Content.Server.GameTicking;
using Content.Server.Gateway.Systems;
using Content.Server.Popups;
using Content.Server.Store.Systems;
using Content.Server.Stunnable;
using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Systems;
using Content.Shared.Alert;
using Content.Shared.Body.Components;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Store.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp106.Systems;

public sealed partial class Scp106System : SharedScp106System
{
    [Dependency] private readonly StairsSystem _stairs = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly FixedPoint2 EssenceRate = 1f;
    private static readonly TimeSpan AddEssenceCooldown = TimeSpan.FromSeconds(1);

    public const int HumansInBackroomsRequiredToAscent = 10;

    public static readonly EntProtoId AscentRule = "Scp106AscentRule";

    private static TimeSpan _defaultOnBackroomsStunTime = TimeSpan.FromSeconds(5f);

    private readonly SoundSpecifier _sendBackroomsSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/onbackrooms.ogg");

    private static readonly TimeSpan PortalSpawnRate = TimeSpan.FromSeconds(60f);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106Component, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent((Entity<Scp106BackRoomMapComponent> _, ref AttemptGatewayOpenEvent args) => args.Cancelled = true);

        #region Phantom

        SubscribeLocalEvent<Scp106PhantomComponent, MobStateChangedEvent>(OnMobStateChangedEvent);
        SubscribeLocalEvent<Scp106PhantomComponent, Scp106ReverseActionEvent>(OnScp106ReverseActionEvent);

        #endregion

        #region Store & Its abilities

        SubscribeLocalEvent<Scp106Component, Scp106ShopAction>(OnShop);

        // Abilities in that store - I love lambdas >:)

        // TODO: Проверка на хендхелд и кенселед
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtPhantomAction args) =>
            _actions.AddAction(ent, args.BoughtAction));
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106OnUpgradePhantomAction args) =>
            ent.Comp.PhantomCoolDown -= args.CooldownReduce);
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtBareBladeAction args) =>
            _actions.AddAction(ent, args.BoughtAction));
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtCreatePortal args) =>
            _actions.AddAction(ent, args.BoughtAction));

        SubscribeLocalEvent<Scp106Component, Scp106BareBladeAction>(OnScp106BareBladeAction);
        SubscribeLocalEvent<Scp106PortalSpawnerComponent, ComponentInit>(OnPortalSpawn);

        #endregion

        #region Portal

        SubscribeLocalEvent<Scp106PortalSpawnerComponent, InteractHandEvent>(OnHandInteract);
        SubscribeLocalEvent<BodyComponent, MobStateChangedEvent>(OnHumanMobStateChanged);

        #endregion

        SubscribeLocalEvent<Scp106Component, Scp106TeleportationDelayActionEvent>(OnTeleportationDelay);
    }

    private void OnMapInit(Entity<Scp106Component> ent, ref MapInitEvent args)
    {
        _alerts.ShowAlert(ent, ent.Comp.Scp106EssenceAlert);

        var marks = SearchForMarks();
        if (marks.Count == 0)
            _ = _stairs.GenerateFloor();

        ent.Comp.NextEssenceAddedTime = _timing.CurTime;

        _defaultOnBackroomsStunTime = ent.Comp.TeleportationDuration;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Пополнение маны для 106го
        var queryScp106 = AllEntityQuery<Scp106Component>();
        while (queryScp106.MoveNext(out var uid, out var component))
        {
            if (component.NextEssenceAddedTime > _timing.CurTime)
                continue;

            component.Essence += EssenceRate;
            Dirty(uid, component);

            component.NextEssenceAddedTime = _timing.CurTime + AddEssenceCooldown;
        }

        // Спавн мобов из портала 106
        CheckPortals();
    }

    #region Abilities

    public override bool PhantomTeleport(Scp106BecomeTeleportPhantomActionEvent args)
    {
        if (args.Args.EventTarget is not {} phantom)
            return false;

        if (!TryComp<Scp106PhantomComponent>(phantom, out var phantomComponent))
            return false;

        if (!_mind.TryGetMind(phantom, out var mindId, out _))
            return false;

        var scp106 = phantomComponent.Scp106BodyUid;

        if (!Exists(scp106))
            return false;

        if (!TryComp<Scp106Component>(scp106, out var scp106Component))
            return false;

        _mind.TransferTo(mindId, scp106);

        var phantomPos = Transform(phantom).Coordinates;

        _transform.SetCoordinates(scp106.Value, phantomPos);

        Del(phantom);

        Scp106FinishTeleportation(scp106.Value, scp106Component.TeleportationDuration);

        return true;
    }

    #endregion

    #region Teleport and related code

    public override async void SendToBackrooms(EntityUid target, Entity<Scp106Component>? scp106 = null)
    {
        // You already here.
        if (HasComp<Scp106BackRoomMapComponent>(Transform(target).MapUid))
            return;

        if (TryComp<Scp106Component>(target, out var scp106Component))
        {
            var a = await GetTransferMark();

            _transform.SetCoordinates(target, a);
            _transform.AttachToGridOrMap(target);

            Scp106FinishTeleportation(target, scp106Component.TeleportationDuration);

            return;
        }

        // Телепортировать только людей
        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        var mark = await GetTransferMark();

        _transform.SetCoordinates(target, mark);
        _transform.AttachToGridOrMap(target);

        _stun.TryParalyze(target, _defaultOnBackroomsStunTime, true);

        _audio.PlayEntity(_sendBackroomsSound, target, target);

        if (scp106 != null)
        {
            AddCurrencyInStore(scp106.Value);
            CheckHumansInBackrooms();
        }
    }

    private bool CheckHumansInBackrooms()
    {
        var humansInBackrooms = CountHumansInBackrooms();

        if (humansInBackrooms < HumansInBackroomsRequiredToAscent)
            return false;

        OnAscent();

        return true;
    }

    public int CountHumansInBackrooms()
    {
        var humansInBackrooms = 0;

        var queryHumans = AllEntityQuery<HumanoidAppearanceComponent, MobStateComponent>();

        while (queryHumans.MoveNext(out var humanUid, out _, out var mobStateComponent))
        {
            if (!HasComp<Scp106BackRoomMapComponent>(Transform(humanUid).MapUid))
                continue;

            if (mobStateComponent.CurrentState != MobState.Alive)
                continue;

            humansInBackrooms += 1;
        }

        return humansInBackrooms;
    }

    private void OnAscent()
    {
        if (_gameTicker.IsGameRuleAdded(AscentRule))
            return;

        _gameTicker.StartGameRule(AscentRule);
    }

    private void Scp106FinishTeleportation(EntityUid uid, TimeSpan teleportationDelay)
    {
        _stun.TryStun(uid, teleportationDelay + TeleportTimeCompensation, true);
        _appearance.SetData(uid, Scp106Visuals.Visuals, Scp106VisualsState.Exiting);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, teleportationDelay, new Scp106TeleportationDelayActionEvent(), uid)
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

        if (TryComp<Scp106Component>(target, out var scp106Component))
        {
            HideBlade((target, scp106Component));
            Scp106FinishTeleportation(target, scp106Component.TeleportationDuration);
        }
    }

    private void OnTeleportationDelay(Entity<Scp106Component> ent, ref Scp106TeleportationDelayActionEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        _appearance.SetData(ent, Scp106Visuals.Visuals, Scp106VisualsState.Default);

        args.Handled = true;
    }

    #endregion

    #region Shop & Its abilities

    private void OnShop(EntityUid uid, Scp106Component component, Scp106ShopAction args)
    {
        if (!TryComp<StoreComponent>(uid, out var store))
            return;

        _store.ToggleUi(uid, uid, store);
    }

    private void OnScp106BareBladeAction(Entity<Scp106Component> ent, ref Scp106BareBladeAction args)
    {
        TryToggleBlade(ent, ref args);
    }

    private bool TryToggleBlade(Entity<Scp106Component> ent, ref Scp106BareBladeAction args, bool force = false)
    {
        if (args.Handled)
            return false;

        // Клинок можно использовать только в карманном измерении или форсированно через код
        if (!CheckIsInDimension(ent) && !force)
        {
            var message = Loc.GetString("scp106-bare-blade-action-invalid");
            _popup.PopupEntity(message, ent, ent);

            return false;
        }

        ToggleBlade(ent, args.Prototype);

        args.Handled = true;
        return true;
    }

    private void ToggleBlade(Entity<Scp106Component> ent, EntProtoId blade)
    {
        // Если клинок уже имеется
        if (ent.Comp.HandTransformed)
        {
            HideBlade(ent);
        }
        else
        {
            EnsureComp<HandsComponent>(ent);
            _hands.AddHand(ent, "right", HandLocation.Middle);
            var sword = Spawn(blade, Transform(ent).Coordinates);

            ent.Comp.Sword = sword;
            _hands.TryPickup(ent, sword, "right");

            ent.Comp.HandTransformed = true;
        }
    }

    public void HideBlade(Entity<Scp106Component> ent)
    {
        Del(ent.Comp.Sword);
        _hands.RemoveHands(ent);
        ent.Comp.HandTransformed = false;
    }

    #endregion

    #region Helpers

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

    #endregion

    private void AddCurrencyInStore(EntityUid uid)
    {
        _store.TryAddCurrency(new Dictionary<string, FixedPoint2> { { "LifeEssence", 2f }, }, uid);
    }
}
