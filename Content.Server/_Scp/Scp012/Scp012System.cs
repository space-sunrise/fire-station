using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Interaction;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Whitelist;
using Content.Shared.Mobs;
using Robust.Server.GameObjects;
using Robust.Server.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using System.Numerics;

namespace Content.Server._Scp.Scp012;

public sealed class Scp012System : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private EntityQuery<Scp012Component> _scpQuery;
    private EntityQuery<Scp012VictimComponent> _victimQuery;
    private EntityQuery<HandsComponent> _handsQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<MutedComponent> _mutedQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    public override void Initialize()
    {
        base.Initialize();

        _scpQuery = GetEntityQuery<Scp012Component>();
        _victimQuery = GetEntityQuery<Scp012VictimComponent>();
        _handsQuery = GetEntityQuery<HandsComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _mutedQuery = GetEntityQuery<MutedComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<Scp012VictimComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<Scp012Component, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<Scp012Component, GotEquippedHandEvent>(OnGotEquipped);
        SubscribeLocalEvent<Scp012VictimComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMapInit(Entity<Scp012Component> ent, ref MapInitEvent args)
    {
        ent.Comp.NextDamageTime = _timing.CurTime;
    }

    private void OnMobStateChanged(Entity<Scp012VictimComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            StopEffect(ent);
    }

    private void OnGotEquipped(Entity<Scp012Component> ent, ref GotEquippedHandEvent args)
    {
        if (!_whitelist.CheckBoth(ent, ent.Comp.Blacklist, ent.Comp.Whitelist))
            return;

        var victimComp = EnsureComp<Scp012VictimComponent>(args.User);
        victimComp.Source = ent;
        _movementSpeed.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshSpeed(Entity<Scp012VictimComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_mobState.IsAlive(ent.Owner) && _hands.IsHolding(ent.Owner, ent.Comp.Source, out _))
            args.ModifySpeed(0f, 0f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var damageTicks = new HashSet<EntityUid>();

        var scpEnumerator = EntityQueryEnumerator<Scp012Component, TransformComponent>();
        while (scpEnumerator.MoveNext(out var uid, out var scp, out var xform))
        {
            if (scp.NextDamageTime == null || _timing.CurTime < scp.NextDamageTime)
                continue;

            scp.NextDamageTime = _timing.CurTime + scp.DamageCooldown;
            damageTicks.Add(uid);

            if (_handsQuery.HasComp(xform.ParentUid))
                continue;

            if (_container.IsEntityInContainer(uid) && _mobStateQuery.HasComp(_xformQuery.GetComponent(xform.ParentUid).ParentUid))
                continue;

            var worldPos = _transform.GetMapCoordinates(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(worldPos, scp.Range))
            {
                if (!_whitelist.CheckBoth(entity, scp.Blacklist, scp.Whitelist))
                    continue;

                if (!_mobState.IsAlive(entity))
                    continue;

                if (_victimQuery.HasComp(entity))
                    continue;

                if (TryComp<BlindableComponent>(entity, out var blind) && blind.IsBlind)
                    continue;

                if (!_interaction.InRangeUnobstructed(entity, uid, scp.Range))
                    continue;

                var vComp = EnsureComp<Scp012VictimComponent>(entity);
                vComp.Source = uid;
            }
        }

        UpdateVictims(frameTime, damageTicks);
    }

    private void UpdateVictims(float frameTime, HashSet<EntityUid> damageTicks)
    {
        var victimEnumerator = EntityQueryEnumerator<Scp012VictimComponent, TransformComponent, PhysicsComponent>();
        while (victimEnumerator.MoveNext(out var vUid, out var victim, out var vXform, out var vPhysics))
        {
            if (!Exists(victim.Source))
                continue;

            if (!_scpQuery.TryComp(victim.Source, out var scp) || !_mobState.IsAlive(vUid))
            {
                StopEffect(vUid);
                continue;
            }

            var victimPos = _transform.GetWorldPosition(vXform);
            var scpPos = _transform.GetWorldPosition(victim.Source.Value);
            var distVec = scpPos - victimPos;
            var distance = distVec.Length();

            if (distance > scp.Range + 1.5f)
            {
                StopEffect(vUid);
                continue;
            }

            if (!_hands.IsHolding(vUid, victim.Source, out _))
                ProcessPulling(vUid, victim, scp, distVec, distance, vPhysics);
            else
                ProcessInHands(vUid, victim, scp, frameTime, damageTicks);
        }
    }

    private void ProcessPulling(EntityUid vUid, Scp012VictimComponent victim, Scp012Component scp, Vector2 distVec, float distance, PhysicsComponent vPhysics)
    {
        if (!Exists(victim.Source))
            return;

        if (_handsQuery.HasComp(_xformQuery.GetComponent(victim.Source.Value).ParentUid))
        {
            StopEffect(vUid);
            return;
        }

        RemCompDeferred<MutedComponent>(vUid);

        if (victim.NextLosCheckTime == null || _timing.CurTime >= victim.NextLosCheckTime)
        {
            victim.NextLosCheckTime = _timing.CurTime + TimeSpan.FromSeconds(0.5);
            victim.CachedLos = _interaction.InRangeUnobstructed(vUid, victim.Source.Value, scp.Range);
        }

        if (victim.CachedLos)
        {
            _transform.SetWorldRotation(vUid, distVec.ToWorldAngle());
            var magnetForce = distVec.Normalized() * scp.AttractionForce * vPhysics.Mass;
            _physics.ApplyLinearImpulse(vUid, magnetForce, body: vPhysics);

            if (distance < 0.6f)
            {
                _hands.TryPickupAnyHand(vUid, victim.Source.Value);
                _movementSpeed.RefreshMovementSpeedModifiers(vUid);
            }
        }
    }

    private void ProcessInHands(EntityUid vUid, Scp012VictimComponent victim, Scp012Component scp, float frameTime, HashSet<EntityUid> damageTicks)
    {
        if (!Exists(victim.Source))
            return;

        EnsureComp<MutedComponent>(vUid);

        victim.TotalTime += frameTime;
        victim.SpeakTimer += frameTime;

        if (damageTicks.Contains(victim.Source.Value))
            _damageable.TryChangeDamage(vUid, scp.Damage, ignoreResistances: true);

        if (victim.SpeakTimer >= 4.0f)
        {
            victim.SpeakTimer = 0f;
            if (_random.Prob(0.7f))
                ForceSpeak(vUid, _random.Pick(victim.Phrases));
        }

        if (victim.TotalTime >= scp.SuicideTimer)
        {
            ForceSpeak(vUid, "scp012-phrase-final");
            var deathDamage = new DamageSpecifier();

            foreach (var key in scp.Damage.DamageDict.Keys)
            {
                deathDamage.DamageDict.Add(key, FixedPoint2.New(200));
            }

            _damageable.TryChangeDamage(vUid, deathDamage, ignoreResistances: true);
            StopEffect(vUid);
        }
    }

    private void ForceSpeak(EntityUid victim, string phraseId)
    {
        var message = Loc.GetString(phraseId);

        RemComp<MutedComponent>(victim);
        _chat.TrySendInGameICMessage(victim, message, InGameICChatType.Speak, hideChat: false);
        EnsureComp<MutedComponent>(victim);
    }

    private void StopEffect(EntityUid uid)
    {
        RemCompDeferred<MutedComponent>(uid);
        RemCompDeferred<Scp012VictimComponent>(uid);

        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }
}
