using Content.Shared.Mobs.Systems;
using Content.Shared.Hands;
using Content.Shared.Movement.Systems;
using Content.Shared.Whitelist;
using Robust.Server.GameObjects;
using Robust.Shared.Timing;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp012;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server._Scp.Scp012;

// TODO: Больше предикшена
// TODO: Перенести систему притягивания и форсированного подбирания для SCP-035.
public sealed partial class Scp012System : SharedScp012System
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private EntityQuery<Scp012Component> _scpQuery;
    private EntityQuery<Scp012VictimComponent> _victimQuery;

    private readonly HashSet<Entity<HandsComponent>> _cachedEntities = [];

    public override void Initialize()
    {
        base.Initialize();

        _scpQuery = GetEntityQuery<Scp012Component>();
        _victimQuery = GetEntityQuery<Scp012VictimComponent>();

        InitializeVictim();

        SubscribeLocalEvent<Scp012Component, GotEquippedHandEvent>(OnGotEquipped);

        SubscribeLocalEvent<Scp012Component, GettingDroppedAttemptEvent>(OnGettingDropped);
        SubscribeLocalEvent<Scp012Component, EntParentChangedMessage>(OnParentChanged);

        SubscribeLocalEvent<Scp012Component, ComponentShutdown>(OnShutdown);
    }

    #region Event handlers

    private void OnGotEquipped(Entity<Scp012Component> ent, ref GotEquippedHandEvent args)
    {
        var victimComp = EnsureComp<Scp012VictimComponent>(args.User);
        victimComp.Source = ent;

        _movementSpeed.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnGettingDropped(Entity<Scp012Component> ent, ref GettingDroppedAttemptEvent args)
    {
        if (_victimQuery.HasComp(ent))
            args.Cancelled = true;
    }

    private void OnParentChanged(Entity<Scp012Component> ent, ref EntParentChangedMessage args)
    {
        if (!_victimQuery.TryComp(args.OldParent, out var victim))
            return;

        SetNextLosCheckTime((args.OldParent.Value, victim));
    }

    private void OnShutdown(Entity<Scp012Component> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp012VictimComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Source != ent)
                continue;

            RemCompDeferred<Scp012VictimComponent>(uid);
        }
    }

    #endregion

    #region Update

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateScp();
        UpdateVictims();
    }

    private void UpdateScp()
    {
        var query = EntityQueryEnumerator<Scp012Component>();
        while (query.MoveNext(out var uid, out var scp))
        {
            _cachedEntities.Clear();
            _lookup.GetEntitiesInRange(_transform.GetMapCoordinates(uid),
                scp.Range,
                _cachedEntities,
                LookupFlags.Dynamic | LookupFlags.Approximate);

            foreach (var ent in _cachedEntities)
            {
                if (_victimQuery.HasComp(ent))
                    continue;

                if (!_whitelist.CheckBoth(ent, scp.Blacklist, scp.Whitelist))
                    continue;

                if (!_mobState.IsAlive(ent))
                    continue;

                if (!_proximity.IsRightType(uid, ent, LineOfSightBlockerLevel.None))
                    continue;

                var victim = EnsureComp<Scp012VictimComponent>(ent);
                victim.Source = uid;
            }
        }
    }

    #endregion
}
