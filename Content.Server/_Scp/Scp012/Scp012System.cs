using Content.Server.Chat.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Containers;

namespace Content.Server._Scp.Scp012;

public sealed class Scp012System : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

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

    private void OnMapInit(EntityUid uid, Scp012Component component, MapInitEvent args)
    {
        component.NextDamageTime = _timing.CurTime;
    }

    private void OnMobStateChanged(EntityUid uid, Scp012VictimComponent component, MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            StopEffect(uid);
    }

    private void OnGotEquipped(EntityUid uid, Scp012Component component, GotEquippedHandEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.User))
            return;

        var victimComp = EnsureComp<Scp012VictimComponent>(args.User);
        victimComp.Source = uid;
        _movementSpeed.RefreshMovementSpeedModifiers(args.User);
    }

    private void OnRefreshSpeed(EntityUid uid, Scp012VictimComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_mobState.IsAlive(uid) && _hands.IsHolding(uid, component.Source, out _))
            args.ModifySpeed(0f, 0f);
    }

    private void ForceSpeak(EntityUid victim, string phraseId)
    {
        var message = Loc.GetString(phraseId);
        bool wasMuted = _mutedQuery.HasComp(victim);
        if (wasMuted) RemComp<MutedComponent>(victim);
        _chat.TrySendInGameICMessage(victim, message, InGameICChatType.Speak, hideChat: false);
        if (wasMuted) EnsureComp<MutedComponent>(victim);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var damageTicks = new HashSet<EntityUid>();

        _scpQuery = GetEntityQuery<Scp012Component>();
        _victimQuery = GetEntityQuery<Scp012VictimComponent>();
        _handsQuery = GetEntityQuery<HandsComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _mutedQuery = GetEntityQuery<MutedComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        var scpEnumerator = EntityQueryEnumerator<Scp012Component, TransformComponent>();
        while (scpEnumerator.MoveNext(out var uid, out var scp, out var xform))
        {
            if (scp.NextDamageTime == null || curTime < scp.NextDamageTime)
                continue;

            scp.NextDamageTime = curTime + scp.DamageCooldown;
            damageTicks.Add(uid);

            if (_handsQuery.HasComp(xform.ParentUid)) continue;
            if (_container.IsEntityInContainer(uid) && _mobStateQuery.HasComp(_xformQuery.GetComponent(xform.ParentUid).ParentUid)) continue;

            var worldPos = _transform.GetMapCoordinates(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(worldPos, scp.Range))
            {
                if (!HasComp<HumanoidAppearanceComponent>(entity) || !_mobState.IsAlive(entity) || _victimQuery.HasComp(entity))
                    continue;

                if (TryComp<BlindableComponent>(entity, out var blind) && blind.IsBlind) continue;
                if (!_interaction.InRangeUnobstructed(entity, uid, scp.Range)) continue;

                var vComp = EnsureComp<Scp012VictimComponent>(entity);
                vComp.Source = uid;
            }
        }

        var victimEnumerator = EntityQueryEnumerator<Scp012VictimComponent, TransformComponent, PhysicsComponent>();
        while (victimEnumerator.MoveNext(out var vUid, out var victim, out var vXform, out var vPhysics))
        {
            if (!_scpQuery.TryComp(victim.Source, out var scp) || !_mobState.IsAlive(vUid))
            {
                StopEffect(vUid);
                continue;
            }

            var victimPos = _transform.GetWorldPosition(vXform);
            var scpPos = _transform.GetWorldPosition(victim.Source);
            var distVec = scpPos - victimPos;
            var distance = distVec.Length();

            if (distance > scp.Range + 1.5f) { StopEffect(vUid); continue; }

            if (!_hands.IsHolding(vUid, victim.Source, out _))
            {
                if (_handsQuery.HasComp(_xformQuery.GetComponent(victim.Source).ParentUid)) { StopEffect(vUid); continue; }
                if (_mutedQuery.HasComp(vUid)) RemCompDeferred<MutedComponent>(vUid);

                if (victim.NextLosCheckTime == null || curTime >= victim.NextLosCheckTime)
                {
                    victim.NextLosCheckTime = curTime + TimeSpan.FromSeconds(0.5);
                    victim.CachedLos = _interaction.InRangeUnobstructed(vUid, victim.Source, scp.Range);
                }

                if (victim.CachedLos)
                {
                    _transform.SetWorldRotation(vUid, distVec.ToWorldAngle());
                    var magnetForce = distVec.Normalized() * scp.AttractionForce * vPhysics.Mass;
                    _physics.ApplyLinearImpulse(vUid, magnetForce, body: vPhysics);

                    if (distance < 0.6f)
                    {
                        _hands.TryPickupAnyHand(vUid, victim.Source);
                        _movementSpeed.RefreshMovementSpeedModifiers(vUid);
                    }
                }
            }
            else
            {
                if (!_mutedQuery.HasComp(vUid)) EnsureComp<MutedComponent>(vUid);

                victim.TotalTime += frameTime;
                victim.SpeakTimer += frameTime;

                if (damageTicks.Contains(victim.Source))
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
                        deathDamage.DamageDict.Add(key, FixedPoint2.New(200)); 
                    _damageable.TryChangeDamage(vUid, deathDamage, ignoreResistances: true);
                    StopEffect(vUid);
                }
            }
        }
    }

    private void StopEffect(EntityUid uid)
    {
        RemCompDeferred<MutedComponent>(uid);
        RemCompDeferred<Scp012VictimComponent>(uid);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }
}