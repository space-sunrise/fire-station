using Content.Server.Popups;
using Content.Server.Chat.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Interaction;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Muting;
using Content.Shared.Humanoid;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp012;

public sealed class Scp012System : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    private readonly string[] _suicidePhrases = 
    {
        "Ещё немного!", "Почему этого не хватает...", "Да!", 
        "Ещё несколько штрихов...", "Она должна быть закончена!", "Замечательно, но..."
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SCP012VictimComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnRefreshSpeed(EntityUid uid, SCP012VictimComponent component, RefreshMovementSpeedModifiersEvent args)
    {
        if (_hands.IsHolding(uid, component.Source, out _))
            args.ModifySpeed(0f, 0f);
    }

    private void ForceSpeak(EntityUid victim, string message)
    {
        bool wasMuted = HasComp<MutedComponent>(victim);
        if (wasMuted) RemComp<MutedComponent>(victim);
        _chat.TrySendInGameICMessage(victim, message, InGameICChatType.Speak, hideChat: false);
        if (wasMuted) EnsureComp<MutedComponent>(victim);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // поиск людей
        var scpQuery = EntityQueryEnumerator<SCP012Component, TransformComponent>();
        while (scpQuery.MoveNext(out var uid, out var scp, out var xform))
        {
            scp.DamageTimer += frameTime;
            bool shouldDamage = scp.DamageTimer >= 0.5f;
            if (shouldDamage) scp.DamageTimer = 0f;

            // если уже кто то держит то другим не надо этого
            if (HasComp<MobStateComponent>(xform.ParentUid))
                continue;

            var worldPos = _transform.GetMapCoordinates(uid);
            foreach (var entity in _lookup.GetEntitiesInRange(worldPos, scp.Range))
            {
                if (!HasComp<HumanoidAppearanceComponent>(entity) || !_mobState.IsAlive(entity))
                    continue;

                if (HasComp<SCP012VictimComponent>(entity))
                    continue;

                if (TryComp<BlindableComponent>(entity, out var blind) && blind.IsBlind)
                    continue;

                if (!_interaction.InRangeUnobstructed(entity, uid, scp.Range))
                    continue;

                var victimComp = EnsureComp<SCP012VictimComponent>(entity);
                victimComp.Source = uid;
            }
        }

        var victimQuery = EntityQueryEnumerator<SCP012VictimComponent, TransformComponent, PhysicsComponent>();
        while (victimQuery.MoveNext(out var vUid, out var victim, out var vXform, out var vPhysics))
        {

            if (!Exists(victim.Source) || !TryComp<SCP012Component>(victim.Source, out var scp))
            {
                StopEffect(vUid);
                continue;
            }

            if (!_mobState.IsAlive(vUid))
            {
                StopEffect(vUid);
                continue;
            }

            var victimPos = _transform.GetWorldPosition(vXform);
            var scpPos = _transform.GetWorldPosition(victim.Source);
            var distVec = scpPos - victimPos;
            var distance = distVec.Length();

            // Если слишком далеко от своего источника
            if (distance > scp.Range + 1.5f)
            {
                StopEffect(vUid);
                continue;
            }

            bool isInHands = _hands.IsHolding(vUid, victim.Source, out _);

            if (!isInHands)
            {
                RemCompDeferred<MutedComponent>(vUid);
                if (_interaction.InRangeUnobstructed(vUid, victim.Source, scp.Range))
                {
                    _transform.SetWorldRotation(vUid, distVec.ToWorldAngle());
                    var direction = distVec.Normalized();
                    var magnetForce = direction * scp.AttractionForce * vPhysics.Mass;
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
                EnsureComp<MutedComponent>(vUid);
                victim.TotalTime += frameTime;
                victim.SpeakTimer += frameTime;

                if (scp.DamageTimer <= 0f)
                {
                    _damageable.TryChangeDamage(vUid, scp.Damage, ignoreResistances: true);
                    
                    if (victim.SpeakTimer >= 4.0f)
                    {
                        victim.SpeakTimer = 0f;
                        if (_random.Prob(0.7f))
                            ForceSpeak(vUid, _random.Pick(_suicidePhrases));
                    }
                }

                if (victim.TotalTime >= scp.SuicideThreshold)
                {
                    ForceSpeak(vUid, "ЭТО НЕВОЗМОЖНО!!!");
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
        RemCompDeferred<SCP012VictimComponent>(uid);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }
}