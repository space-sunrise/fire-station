using System.Diagnostics.CodeAnalysis;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Other.EmitSoundRandomly;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.ScpMask;
using Content.Shared._Scp.Watching;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Rejuvenate;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System : EntitySystem
{
    [Dependency] private readonly PredictedRandomSystem _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId StatusEffectSleep = "StatusEffectForcedSleeping";
    private static readonly SoundSpecifier StorageOpenSound = new SoundCollectionSpecifier("MetalBreak");

    protected EntityQuery<Scp096Component> Scp096Query;
    protected EntityQuery<ActiveScp096HeatingUpComponent> HeatingUpQuery;
    protected EntityQuery<ActiveScp096RageComponent> RageQuery;
    protected EntityQuery<ActiveScp096WithoutFaceComponent> WithoutFaceQuery;
    protected EntityQuery<Scp096FaceComponent> FaceQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, SimpleEntitySeenEvent>(OnSeen);

        SubscribeLocalEvent<ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<Scp096Component, HitScanAttackedEvent>(OnHitScanHit);
        SubscribeLocalEvent<Scp096Component, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<Scp096Component, DisarmedEvent>(OnDisarmed);

        SubscribeLocalEvent<Scp096Component, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp096Component, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<Scp096Component, SleepStateChangedEvent>(OnSleepStateChanged);

        SubscribeLocalEvent<Scp096Component, ScpMaskTargetEquipAttempt>(OnMaskAttempt);
        SubscribeLocalEvent<Scp096Component, RejuvenateEvent>(OnRejuvenate);

        SubscribeLocalEvent<Scp096Component, ComponentInit>(OnInit);
        SubscribeLocalEvent<Scp096Component, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<Scp096Component, BeforeRandomlyEmittingSoundEvent>(OnEmitSoundRandomly);

        InitializeRage();
        InitializeTargets();
        InitializeHands();
        InitializeActions();
        InitializeWithoutFace();
        InitializeAppearance();

        Scp096Query = GetEntityQuery<Scp096Component>();
        HeatingUpQuery = GetEntityQuery<ActiveScp096HeatingUpComponent>();
        RageQuery = GetEntityQuery<ActiveScp096RageComponent>();
        WithoutFaceQuery = GetEntityQuery<ActiveScp096WithoutFaceComponent>();
        FaceQuery = GetEntityQuery<Scp096FaceComponent>();

        Log.Level = LogLevel.Verbose;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        UpdateHeatingUp();
        UpdateRage();
        UpdateAnimations();
        UpdateJittering();
    }

    #region Event handlers

    private void OnSeen(Entity<Scp096Component> ent, ref SimpleEntitySeenEvent args)
    {
        if (WithoutFaceQuery.HasComp(ent))
            return;

        TryAddTarget(ent, args.Viewer);
    }

    private void OnProjectileHit(ref ProjectileHitEvent args)
    {
        CheckRandomTargetByDamage(args.Target, args.Shooter, 1f);
    }

    private void OnHitScanHit(Entity<Scp096Component> ent, ref HitScanAttackedEvent args)
    {
        CheckRandomTargetByDamage(ent, args.User, 1f);
    }

    private void OnAttacked(Entity<Scp096Component> ent, ref AttackedEvent args)
    {
        var chance = _random.NextFloatForEntity(ent, 0.4f, 0.7f);
        CheckRandomTargetByDamage(ent, args.User, chance);
    }

    private void OnDisarmed(Entity<Scp096Component> ent, ref DisarmedEvent args)
    {
        CheckRandomTargetByDamage(ent, args.Source, 0.1f);
    }

    /// <summary>
    /// Отвечает за логику агрессии после удара по скромнику.
    /// В случае успешного прохождения проверок записывает нападающего в цели scp-096
    /// </summary>
    private void CheckRandomTargetByDamage(EntityUid target, EntityUid? attacker, float chance)
    {
        if (!attacker.HasValue)
            return;

        if (!TryComp<Scp096Component>(target, out var scp))
            return;

        if (!_random.ProbForEntity(attacker.Value, chance))
            return;

        TryAddTarget((target, scp), attacker.Value, true);
    }

    private void OnMobStateChanged(Entity<Scp096Component> ent, ref MobStateChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        UpdateAppearance(ent.AsNullable());

        if (args.NewMobState == MobState.Alive)
            return;

        RemoveAllTargets(ent);
    }

    private void OnSleepStateChanged(Entity<Scp096Component> ent, ref SleepStateChangedEvent args)
    {
        ent.Comp.DeadToIdleAnimation = !args.FellAsleep;
        ent.Comp.AgroToDeadAnimation = args.FellAsleep;
        Dirty(ent);

        UpdateAppearance(ent.AsNullable());
        AddToPendingAnimations(ent, _timing.CurTime + ent.Comp.AnimationDuration);
    }

    private void OnAttackAttempt(Entity<Scp096Component> ent, ref AttackAttemptEvent args)
    {
        if (!args.Target.HasValue)
            return;

        var target = args.Target.Value;

        if (!CanAttack(ent, target))
        {
            args.Cancel();
            return;
        }

        // Открываем ударом шкафчики и хранилища
        if (TryComp<EntityStorageComponent>(target, out var entityStorageComponent) && !entityStorageComponent.Open)
        {
            _lock.TryUnlock(target, ent);
            _entityStorage.OpenStorage(target, entityStorageComponent);
            _audio.PlayLocal(StorageOpenSound, ent, ent);
        }
    }

    private void OnMaskAttempt(Entity<Scp096Component> ent, ref ScpMaskTargetEquipAttempt args)
    {
        if (HasComp<ActiveScp096RageComponent>(ent) || HasComp<ActiveScp096HeatingUpComponent>(ent))
            args.Cancel();
    }

    private void OnRejuvenate(Entity<Scp096Component> ent, ref RejuvenateEvent args)
    {
        RemComp<ActiveScp096WithoutFaceComponent>(ent);
        RemComp<ActiveScp096RageComponent>(ent);
        RemComp<ActiveScp096HeatingUpComponent>(ent);

        RemoveAllTargets(ent);
    }

    protected virtual void OnEmitSoundRandomly(Entity<Scp096Component> ent, ref BeforeRandomlyEmittingSoundEvent args)
    {
        if (HasComp<ActiveScp096RageComponent>(ent) || HasComp<SleepingComponent>(ent) || HasComp<ActiveScp096HeatingUpComponent>(ent))
            args.Cancel();
    }

    protected virtual void OnInit(Entity<Scp096Component> ent, ref ComponentInit args)
    {
        UpdateAudio(ent.AsNullable(), ent.Comp.CrySound);
        _meta.AddFlag(ent, MetaDataFlags.PvsPriority);
    }

    protected virtual void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            RemCompDeferred<Scp096TargetComponent>(uid);
        }

        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);
        Dirty(ent);

        _meta.RemoveFlag(ent, MetaDataFlags.PvsPriority);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Проверяет, может ли scp-096 перейти в состояние ярости.
    /// Не включает проверку, что скромник уже в состоянии ярости. Так и задуманно
    /// </summary>
    private bool CanBeAggro(Entity<Scp096Component> ent, bool ignoreMask = false)
    {
        if (HasComp<SleepingComponent>(ent))
            return false;

        if (_mobState.IsIncapacitated(ent))
            return false;

        // В маске мы мирные
        if (_scpMask.HasScpMask(ent) && !ignoreMask)
            return false;

        return true;
    }

    /// <summary>
    /// Обновляет модификаторы скорости передвижения scp-096 в зависимости от текущего состояния.
    /// В режиме ярости скорость большая, в обычном состоянии маленькая.
    /// </summary>
    private void RefreshSpeedModifiers(Entity<Scp096Component?> ent, bool forceDefault = false)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        float newSpeed;

        if (RageQuery.TryComp(ent, out var rage) && !forceDefault)
            newSpeed = rage.Speed;
        else if (WithoutFaceQuery.TryComp(ent, out var withoutFaceComp) && !forceDefault)
            newSpeed = withoutFaceComp.Speed;
        else
            newSpeed = ent.Comp.Speed;

        _speedModifier.ChangeBaseSpeed(ent, newSpeed, newSpeed, 20.0f);
    }

    private void UpdateAudio(Entity<Scp096Component?> ent, SoundSpecifier? sound = null)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        sound ??= ent.Comp.CrySound;

        ent.Comp.AudioStream = _audio.Stop(ent.Comp.AudioStream);

        if (_net.IsServer)
            ent.Comp.AudioStream = _audio.PlayPvs(sound, ent)?.Entity;

        if (ent.Comp.AudioStream != null)
            _transform.SetParent(ent.Comp.AudioStream.Value, ent);

        Dirty(ent);
    }

    /// <summary>
    /// Проверяет, может ли скромник ударить эту сущность.
    /// Можно ударить свою цель, структуру, борга или животного.
    /// </summary>
    private bool CanAttack(Entity<Scp096Component> scp, EntityUid target)
    {
        // В состоянии содранного лица можно атаковать без ограничений
        if (HasComp<ActiveScp096WithoutFaceComponent>(scp))
            return true;

        // Атаковать можно только в состоянии агрессии или случае выше
        if (!HasComp<ActiveScp096RageComponent>(scp))
            return false;

        // Если цель не имеет компонента моргания, значит это 99% не игрок
        // И скромник имеет право это расхуярить(это структура, борг или животное)
        if (!HasComp<MobStateComponent>(target))
            return true;

        if (!HasComp<Scp096TargetComponent>(target))
            return false;

        return true;
    }

    private bool TryGetFace(Entity<Scp096Component?> ent, [NotNullWhen(true)] out Entity<Scp096FaceComponent>? face)
    {
        face = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!Exists(ent.Comp.FaceEntity))
        {
            Log.Error($"Found SCP-096 without valid face entity. Scp096 is {ToPrettyString(ent)}, while reference is {ToPrettyString(ent.Comp.FaceEntity)}");
            return false;
        }

        var faceEntity = ent.Comp.FaceEntity.Value;

        if (!FaceQuery.TryComp(faceEntity, out var faceComp))
        {
            Log.Error($"Found SCP-096 face without {nameof(Scp096FaceComponent)}! Prototype: {Prototype(faceEntity)}, Entity: {ToPrettyString(faceEntity)}");
            return false;
        }

        face = (faceEntity, faceComp);
        return true;
    }

    private bool TryGetScp096FromFace(Entity<Scp096FaceComponent> ent, [NotNullWhen(true)] out Entity<Scp096Component>? scp096)
    {
        scp096 = null;

        if (!Exists(ent.Comp.FaceOwner))
        {
            Log.Error($"Found SCP-096 face entity with unexisting owner. Face - {ToPrettyString(ent)}, Owner - {ToPrettyString(ent.Comp.FaceOwner)}");
            return false;
        }

        if (!Scp096Query.TryComp(ent.Comp.FaceOwner, out var scp096Comp))
        {
            Log.Error($"Found SCP-096 face owner without {nameof(Scp096Component)}. Face - {ToPrettyString(ent)}, Owner - {ToPrettyString(ent.Comp.FaceOwner)}");
            return false;
        }

        scp096 = (ent.Comp.FaceOwner.Value, scp096Comp);
        return true;
    }

    private void ToggleMovement(EntityUid uid, bool enable)
    {
        if (enable)
        {
            RemComp<BlockMovementComponent>(uid);
            RemComp<NoRotateOnInteractComponent>(uid);
        }
        else
        {
            EnsureComp<BlockMovementComponent>(uid);
            EnsureComp<NoRotateOnInteractComponent>(uid);
        }

        var finalResult = _actionBlocker.UpdateCanMove(uid);

        if (enable != finalResult)
            Log.Error($"Movement state mismatch! Tried to set movement to {enable}, but ended up with {finalResult}.");
    }

    private bool TryToggleTears(Entity<Scp096Component?> ent, bool value)
    {
        if (!TryGetFace(ent, out var face))
            return false;

        ToggleTears(face.Value, value);
        return true;
    }

    private bool TryToggleTearsReagent(Entity<Scp096Component?> ent, bool useDefaultReagent)
    {
        if (!TryGetFace(ent, out var face))
            return false;

        ToggleTearsReagent(face.Value, useDefaultReagent);
        return true;
    }

    private bool TryModifyTearsSpawnSpeed(Entity<Scp096Component?> ent, bool cryFaster)
    {
        if (!TryGetFace(ent, out var face))
            return false;

        ModifyTearsSpawnSpeed(face.Value, cryFaster);
        return true;
    }

    protected virtual void ToggleTears(Entity<Scp096FaceComponent> ent, bool value) { }

    protected virtual void ToggleTearsReagent(Entity<Scp096FaceComponent> ent, bool useDefaultReagent) { }

    protected virtual void ModifyTearsSpawnSpeed(Entity<Scp096FaceComponent> ent, bool cryFaster) { }

    #endregion
}

#region Events

/// <summary>
/// Ивент, информирующий клиент, что требуется перепроверять внешний вид scp-096.
/// </summary>
/// <param name="netEntity"><see cref="NetEntity"/> scp-096</param>
[Serializable, NetSerializable]
public sealed class Scp096RequireUpdateVisualsEvent(NetEntity netEntity) : EntityEventArgs
{
    public NetEntity NetEntity = netEntity;
}

public sealed partial class Scp096CryOutEvent : InstantActionEvent;
public sealed partial class Scp096FaceSkinRipEvent : InstantActionEvent;
public sealed partial class Scp096SitDownEvent : InstantActionEvent;


[Serializable, NetSerializable]
public sealed partial class Scp096FaceSkinRipStartDoAfterEvent : SimpleDoAfterEvent;

#endregion
