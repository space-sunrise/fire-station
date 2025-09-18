using System.Linq;
using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Ghost;
using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.Systems;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._Scp.Scp035;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Item;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Light.Components;
using Content.Shared.Maps;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Scp.Scp035;

// TODO: АНХАРДКОД
public sealed class Scp035System : SharedScp035System
{
    [Dependency] private readonly HTNSystem _htn = default!;
    [Dependency] private readonly NPCSystem _npc = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly TileSystem _tile = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private List<string> _messages = new()
    {
        "Ну же, надень меня!",
        "Чего ты ждешь! Надень меня..",
        "Помоги мне!.."
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp035MaskComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<Scp035MaskUserComponent, MaskRaiseArmyActionEvent>(OnRaiseArmy);
        SubscribeLocalEvent<Scp035MaskUserComponent, AfterPointedAtEvent>(OnPointedAt);
    }

    private void OnMapInit(Entity<Scp035MaskComponent> ent, ref MapInitEvent args)
    {
        var toggleUsed = new ItemToggledEvent(false, true, null);
        RaiseLocalEvent(ent, ref toggleUsed);
    }

    private void OnRaiseArmy(Entity<Scp035MaskUserComponent> ent, ref MaskRaiseArmyActionEvent args)
    {
        if (args.Handled)
            return;

        if (ent.Comp.Servants.Count >= ent.Comp.MaxServants)
        {
            _popup.PopupEntity("Достигнуто максимальное число!", ent, ent, PopupType.MediumCaution);
            return;
        }

        var servant = Spawn("MobServant035", Transform(ent).Coordinates);
        var comp = EnsureComp<Scp035ServantComponent>(servant);
        comp.User = ent;
        Dirty(servant, comp);

        ent.Comp.Servants.Add(servant);
        _npc.SetBlackboard(servant, NPCBlackboard.FollowTarget, new EntityCoordinates(ent, Vector2.Zero));
        UpdateServantNpc(servant, ent.Comp.CurrentOrder);

        args.Handled = true;
    }

    private void OnPointedAt(Entity<Scp035MaskUserComponent> ent, ref AfterPointedAtEvent args)
    {
        if (ent.Comp.CurrentOrder != MaskOrderType.Kill)
            return;

        foreach (var servant in ent.Comp.Servants)
        {
            _npc.SetBlackboard(servant, NPCBlackboard.CurrentOrderedTarget, args.Pointed);
        }
    }

    public override void UpdateServantNpc(EntityUid uid, MaskOrderType orderType)
    {
        if (!TryComp<HTNComponent>(uid, out var htn))
            return;

        if (htn.Plan != null)
            _htn.ShutdownPlan(htn);

        _npc.SetBlackboard(uid, NPCBlackboard.CurrentOrders, orderType);
        _htn.Replan(htn);
    }


    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<Scp035MaskComponent>();
        while (query.MoveNext(out var uid, out var maskComponent))
        {
            if (maskComponent.User.HasValue)
                continue;

            HandleMessaging((uid, maskComponent));
            HandleLiquidSpawning((uid, maskComponent));
        }
    }

    private void HandleMessaging(Entity<Scp035MaskComponent> entity)
    {
        var curTime = _gameTiming.CurTime;

        if (curTime < entity.Comp.NextMessaging)
            return;

        var message = _random.Pick(_messages);
        _chatSystem.TrySendInGameICMessage(entity, message, InGameICChatType.Speak, ChatTransmitRange.Normal, ignoreActionBlocker: true);

        entity.Comp.NextMessaging = curTime + TimeSpan.FromSeconds(60);
    }

    private void HandleLiquidSpawning(Entity<Scp035MaskComponent> entity)
    {
        var curTime = _gameTiming.CurTime;
        if (curTime < entity.Comp.NextLiquidSpawning)
            return;

        var coords = Transform(entity).Coordinates;

        var tempSol = new Solution();
        tempSol.AddReagent("PoisonWine", 25);
        _puddle.TrySpillAt(coords, tempSol, out _);

        FixedPoint2 total = 0;
        var puddles = _lookup.GetEntitiesInRange<PuddleComponent>(coords, 5).ToList();
        foreach (var puddle in puddles)
        {
            if (!puddle.Comp.Solution.HasValue)
                continue;

            var allReagents = puddle.Comp.Solution.Value.Comp.Solution.GetReagentPrototypes(_prototypeManager);
            total = allReagents.Where(reagent => reagent.Key.ID == "PoisonWine").Aggregate(total, (current, reagent) => current + reagent.Value);
        }

        if (total >= 200)
        {
            var xform = Transform(entity);
            if (!TryComp<MapGridComponent>(xform.GridUid, out var map))
                return;
            var tiles = map.GetTilesIntersecting(Box2.CenteredAround(_transformSystem.GetWorldPosition(xform),
                new Vector2(3 * 2, 3)))
                .ToArray();

            _random.Shuffle(tiles);

            for (var i = 0; i < 10; i++)
            {
                if (!tiles.TryGetValue(i, out var value))
                    continue;

                _tile.PryTile(value);
            }

            var lookup = _lookup.GetEntitiesInRange(entity, 3, LookupFlags.Approximate | LookupFlags.Static);
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
                        dspec.DamageDict.Add("Structural", 60);
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
        }

        entity.Comp.NextLiquidSpawning = curTime + TimeSpan.FromSeconds(60);
    }
}
