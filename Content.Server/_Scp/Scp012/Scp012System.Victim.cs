using System.Numerics;
using Content.Server.Chat.Systems;
using Content.Server.Hands.Systems;
using Content.Shared._Scp.Proximity;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Speech.Muting;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server._Scp.Scp012;

public sealed partial class Scp012System
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private void InitializeVictim()
    {
        SubscribeLocalEvent<Scp012VictimComponent, MapInitEvent>(OnVictimMapInit);
        SubscribeLocalEvent<Scp012VictimComponent, ComponentShutdown>(OnVictimShutdown);

        SubscribeLocalEvent<Scp012VictimComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<Scp012VictimComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    #region Event handlers

    private void OnVictimMapInit(Entity<Scp012VictimComponent> ent, ref MapInitEvent args)
    {
        EnsureComp<MutedComponent>(ent);

        SetNextSuicideTime(ent);
        SetNextSpeakTime(ent);
        SetNextPassiveDamageTime(ent);
        SetNextLosCheckTime(ent);
    }

    private void OnVictimShutdown(Entity<Scp012VictimComponent> ent, ref ComponentShutdown args)
    {
        RemCompDeferred<MutedComponent>(ent);
        _movementSpeed.RefreshMovementSpeedModifiers(ent);
    }

    private void OnMobStateChanged(Entity<Scp012VictimComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsIncapacitated(ent))
            RemCompDeferred<Scp012VictimComponent>(ent);
    }

    private void OnRefreshSpeed(Entity<Scp012VictimComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (_mobState.IsAlive(ent.Owner) && _hands.IsHolding(ent.Owner, ent.Comp.Source, out _))
            args.ModifySpeed(0f, 0f);
    }

    #endregion

    #region Update

    private void UpdateVictims()
    {
        var query = EntityQueryEnumerator<Scp012VictimComponent, TransformComponent, PhysicsComponent>();
        while (query.MoveNext(out var uid, out var victim, out var xform, out var physics))
        {
            if (!_scpQuery.TryComp(victim.Source, out var scp))
                continue;

            var victimPos = _transform.GetWorldPosition(xform);
            var scpPos = _transform.GetWorldPosition(victim.Source.Value);
            var distVec = scpPos - victimPos;
            var distance = distVec.Length();

            if (distance > scp.Range)
            {
                RemCompDeferred<Scp012VictimComponent>(uid);
                continue;
            }

            var victimEntity = (uid, victim);
            var scpEntity = (victim.Source.Value, scp);

            if (!_hands.IsHolding(uid, victim.Source, out _))
                ProcessPulling(victimEntity, scpEntity, distVec, distance, physics);
            else
                ProcessInHands(victimEntity, scpEntity);
        }
    }

    private void ProcessPulling(Entity<Scp012VictimComponent> ent, Entity<Scp012Component> scp, Vector2 distVec, float distance, PhysicsComponent physics)
    {
        // Оптимизации с кешированием наличия преграды между жертвой и нотами.
        if (_timing.CurTime >= ent.Comp.NextLosCheckTime)
        {
            SetNextLosCheckTime(ent);
            ent.Comp.CachedLos = _proximity.IsRightType(scp, ent, LineOfSightBlockerLevel.None);
        }

        // Если между жертвой и нотами преграда - не движемся
        if (!ent.Comp.CachedLos)
            return;

        _transform.SetWorldRotation(ent, distVec.ToWorldAngle());
        var magnetForce = distVec.Normalized() * scp.Comp.AttractionForce * physics.Mass;
        _physics.ApplyLinearImpulse(ent, magnetForce, body: physics);

        // Если мы не достигли дистанции до поднятия - просто движемся дальше
        if (distance >= ent.Comp.PickupDistance)
            return;

        if (!TryPickup(ent, scp))
            return;

        _movementSpeed.RefreshMovementSpeedModifiers(ent);

        ent.Comp.CachedLos = false;
        ent.Comp.NextLosCheckTime = null;
    }

    private void ProcessInHands(Entity<Scp012VictimComponent> ent, Entity<Scp012Component> scp)
    {
        TryPassiveDamage(ent, scp);
        TrySpeak(ent);
        TrySuicide(ent, scp);
    }

    #endregion

    #region Helpers

    private bool TryPickup(Entity<Scp012VictimComponent> ent, Entity<Scp012Component> scp)
    {
        if (_hands.TryPickupAnyHand(ent, scp))
            return true;

        if (!_hands.TryDrop(ent.Owner, Transform(ent).Coordinates))
            return false;

        if (_hands.TryPickupAnyHand(ent, scp))
            return true;

        RemCompDeferred<Scp012VictimComponent>(ent);
        Log.Error($"Entity {ToPrettyString(ent)} failed to pickup {ToPrettyString(scp)}");
        return false;
    }

    private bool TryPassiveDamage(Entity<Scp012VictimComponent> ent, Entity<Scp012Component> scp)
    {
        if (_timing.CurTime < ent.Comp.NextPassiveDamageTime)
            return false;

        SetNextPassiveDamageTime(ent);
        if (_damageable.TryChangeDamage(ent, scp.Comp.PassiveDamage, ignoreResistances: true)?.GetTotal() == FixedPoint2.Zero)
            return false;

        return true;
    }

    private bool TrySpeak(Entity<Scp012VictimComponent> ent)
    {
        if (_timing.CurTime < ent.Comp.NextSpeakTime)
            return false;

        SetNextSpeakTime(ent);

        if (!_random.Prob(ent.Comp.SpeakChance))
            return false;

        ForceSpeak(ent, _random.Pick(ent.Comp.Phrases));
        return true;
    }

    private bool TrySuicide(Entity<Scp012VictimComponent> ent, Entity<Scp012Component> scp)
    {
        if (_timing.CurTime < ent.Comp.NextSuicideTime)
            return false;

        ForceSpeak(ent, "scp012-phrase-final");

        if (_damageable.TryChangeDamage(ent, scp.Comp.SuicideDamage, ignoreResistances: true)?.GetTotal() == FixedPoint2.Zero)
            return false;

        _mobState.ChangeMobState(ent, MobState.Dead, origin: scp);

        return true;
    }

    private void ForceSpeak(EntityUid victim, string phraseId)
    {
        var message = Loc.GetString(phraseId);

        RemComp<MutedComponent>(victim);
        _chat.TrySendInGameICMessage(victim, message, InGameICChatType.Speak, hideChat: false);
        EnsureComp<MutedComponent>(victim);
    }

    private void SetNextSuicideTime(Entity<Scp012VictimComponent> ent)
    {
        ent.Comp.NextSuicideTime = _timing.CurTime + ent.Comp.SuicideCooldown;
    }

    private void SetNextSpeakTime(Entity<Scp012VictimComponent> ent)
    {
        ent.Comp.NextSpeakTime = _timing.CurTime + ent.Comp.SpeakCooldown;
    }

    private void SetNextPassiveDamageTime(Entity<Scp012VictimComponent> ent)
    {
        ent.Comp.NextPassiveDamageTime = _timing.CurTime + ent.Comp.PassiveDamageCooldown;
    }

    private void SetNextLosCheckTime(Entity<Scp012VictimComponent> ent)
    {
        ent.Comp.NextLosCheckTime = _timing.CurTime + ent.Comp.LosCooldown;
    }

    #endregion
}
