using System.Linq;
using System.Threading.Tasks;
using Content.Server._Scp.Helpers;
using Content.Server._Scp.Scp106.Components;
using Content.Server.Actions;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Gateway.Systems;
using Content.Server.Ghost;
using Content.Server.Jittering;
using Content.Server.Speech.EntitySystems;
using Content.Server.Store.Systems;
using Content.Server.Stunnable;
using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Systems;
using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffect;
using Content.Shared.Store.Components;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106System : SharedScp106System
{
    [Dependency] private readonly StairsSystem _stairs = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly StoreSystem _store = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly JitteringSystem _jittering = default!;
    [Dependency] private readonly StutteringSystem _stuttering = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ActionsSystem _actions = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly FixedPoint2 EssenceRate = 1f;
    private static readonly TimeSpan AddEssenceCooldown = TimeSpan.FromSeconds(1);

    private const int HumansInBackroomsRequiredToAscent = 10;
    private static readonly TimeSpan AnnounceAfter = TimeSpan.FromMinutes(5f);

    private static readonly TimeSpan AscentStunTime = TimeSpan.FromSeconds(5f);
    private static readonly TimeSpan AscentJitterTime = TimeSpan.FromSeconds(15f);
    private static readonly TimeSpan AscentStutterTime = TimeSpan.FromSeconds(30f);

    private readonly SoundSpecifier _sendBackroomsSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/onbackrooms.ogg");

    private static readonly EntProtoId PortalPrototype = "Scp106Portal";

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
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var queryScp106 = AllEntityQuery<Scp106Component>();

        // TODO: Поддержка нескольких 106 через хранение значения в компоненте
        while (queryScp106.MoveNext(out var uid, out var component))
        {
            if (component.NextEssenceAddedTime > _timing.CurTime)
                continue;

            component.Essence += EssenceRate;
            Dirty(uid, component);

            component.NextEssenceAddedTime = _timing.CurTime + AddEssenceCooldown;
        }
    }

    #region Phantom

    private void OnMobStateChangedEvent(Entity<Scp106PhantomComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!_mind.TryGetMind(ent, out var mindId, out _))
            return;

        if (!Exists(ent.Comp.Scp106BodyUid))
            return;

        _mind.TransferTo(mindId, ent.Comp.Scp106BodyUid);
        ent.Comp.Scp106BodyUid = null;

        Dirty(ent);
    }

    private void OnScp106ReverseActionEvent(Entity<Scp106PhantomComponent> ent, ref Scp106ReverseActionEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (!_mobState.IsDead(target))
            return;

        if (!Exists(ent.Comp.Scp106BodyUid))
            return;

        var targetPos = Transform(target).Coordinates;

        _transform.SetCoordinates(ent.Comp.Scp106BodyUid.Value, targetPos);
        SendToBackrooms(target);

        if (args.Args.EventTarget == null)
            return;

        _mobState.ChangeMobState(args.Args.EventTarget.Value, MobState.Dead);
    }

    public override void BecomeTeleportPhantom(EntityUid uid, ref Scp106BecomeTeleportPhantomAction args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _))
            return;

        var phantom = Spawn(args.PhantomPrototype, Transform(uid).Coordinates);

        _mind.TransferTo(mindId, phantom);

        if (TryComp<Scp106PhantomComponent>(phantom, out var scp106PhantomComponent))
            scp106PhantomComponent.Scp106BodyUid = uid;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, args.Delay, new Scp106BecomeTeleportPhantomActionEvent(), phantom)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterEventArgs);

        _appearance.SetData(uid, Scp106Visuals.Visuals, Scp106VisualsState.Entering);
    }

    public override void BecomePhantom(Entity<Scp106Component> ent, ref Scp106BecomePhantomAction args)
    {
        var scp106Phantom = Spawn(args.PhantomPrototype, Transform(ent).Coordinates);

        if (_mind.TryGetMind(ent, out var mindId, out _))
            _mind.TransferTo(mindId, scp106Phantom);

        if (!TryComp<Scp106PhantomComponent>(scp106Phantom, out var scp106PhantomComponent))
            return;

        scp106PhantomComponent.Scp106BodyUid = ent;
        Dirty(ent);

        args.Action.Comp.UseDelay = ent.Comp.PhantomCoolDown;
        args.Handled = true;
    }

    #endregion

    #region Abilities

    public override void Scp106SpawnPortal(Entity<Scp106Component> ent, ref Scp106CreatePortalEvent args)
    {
        ent.Comp.Scp106HasPortals ++;

        Dirty(ent);

        var portalUid = Spawn(PortalPrototype, Transform(ent).Coordinates);
        Comp<Scp106PortalSpawnerComponent>(portalUid).Scp106 = ent;
    }

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
        _stun.TryParalyze(target, TimeSpan.FromSeconds(5), true);
        _audio.PlayEntity(_sendBackroomsSound, target, target);

        if (scp106 != null)
        {
            AddCurrencyInStore(scp106.Value);
            CheckHumansInBackrooms(scp106.Value);
        }
    }

    private bool CheckHumansInBackrooms(Entity<Scp106Component> scp106)
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

        if (humansInBackrooms < HumansInBackroomsRequiredToAscent)
            return false;

        Timer.Spawn(AnnounceAfter, () =>
        {
            OnAscent(scp106);
        });

        return true;
    }

    private void OnAscent(Entity<Scp106Component> scp106)
    {
        // TODO: Отдельный режим через геймрул с нашействием через порталы и крутым амбиентом
        var statusEffectQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, StatusEffectsComponent>();
        while (statusEffectQuery.MoveNext(out var human, out _, out _))
        {
            _stun.TryParalyze(human, AscentStunTime, true);
            _jittering.DoJitter(human, AscentJitterTime, true);
            _stuttering.DoStutter(human,AscentStutterTime, true);

            RaiseLocalEvent(human, new GhostBooEvent(), true);
        }

        var message = Loc.GetString("scp106-many-humans-in-backrooms-alarm-announcement");
        _chat.DispatchGlobalAnnouncement(message, colorOverride: Color.Red);

        // TODO: Смена алерта на гамма

        _audio.PlayGlobal(
            "/Audio/_Sunrise/stab.ogg",
            Filter.Broadcast(),
            false,
            new AudioParams().WithVolume(-10));
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
            Scp106FinishTeleportation(target, scp106Component.TeleportationDuration);
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
        // Если клинок уже имеется
        if (ent.Comp.HandTransformed)
        {
            Del(ent.Comp.Sword);
            _hands.RemoveHands(ent);
            ent.Comp.HandTransformed = false;

            return;
        }

        EnsureComp<HandsComponent>(ent);
        _hands.AddHand(ent, "right", HandLocation.Middle);
        var sword = Spawn(args.Prototype, Transform(ent).Coordinates);

        ent.Comp.Sword = sword;
        _hands.TryPickup(ent, sword, "right");

        ent.Comp.HandTransformed = true;
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
