using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.ScpMask;
using Content.Shared._Scp.Watching;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared.Bed.Sleep;
using Content.Shared.CombatMode;
using Content.Shared.Doors.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Lock;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System : EntitySystem
{
    [Dependency] private readonly PredictedRandomSystem _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly FieldOfViewSystem _fov = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _speedModifier = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly LockSystem _lock = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly EntProtoId StatusEffectSleep = "StatusEffectForcedSleeping";

    private static readonly SoundSpecifier StorageOpenSound = new SoundCollectionSpecifier("MetalBreak");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, SimpleEntitySeenEvent>(OnSeen);
        SubscribeLocalEvent<ProjectileHitEvent>(OnProjectileHit);
        SubscribeLocalEvent<Scp096Component, HitScanAttackedEvent>(OnHitScanHit);
        SubscribeLocalEvent<Scp096Component, AttackedEvent>(OnAttacked);
        SubscribeLocalEvent<Scp096Component, DisarmedEvent>(OnDisarmed);

        SubscribeLocalEvent<Scp096Component, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp096Component, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<Scp096Component, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<Scp096Component, SleepStateChangedEvent>(OnSleepStateChanged);

        SubscribeLocalEvent<Scp096Component, ScpMaskTargetEquipAttempt>(OnMaskAttempt);

        SubscribeLocalEvent<Scp096Component, AfterAutoHandleStateEvent>(OnHandleState);
        SubscribeLocalEvent<Scp096Component, ComponentInit>(OnInit);
        SubscribeLocalEvent<Scp096Component, ComponentShutdown>(OnShutdown);

        InitTargets();
    }

    #region Updater

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var ent = (uid, comp);
            UpdateScp096(ent);
        }

        UpdateRage();
    }

    private void UpdateScp096(Entity<Scp096Component> ent)
    {
        if (!ent.Comp.InRageMode)
            return;

        if (!ent.Comp.RageStartTime.HasValue)
            return;

        var currentTime = _timing.CurTime;
        var elapsedTime = currentTime - ent.Comp.RageStartTime.Value;

        if (elapsedTime > ent.Comp.RageDuration)
        {
            OnRageTimeExceeded(ent);
        }
    }

    #endregion

    #region Event handlers

    private void OnSeen(Entity<Scp096Component> ent, ref SimpleEntitySeenEvent args)
    {
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

        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        if (args.NewMobState == MobState.Alive)
            return;

        RemoveAllTargets(ent);
    }

    private void OnSleepStateChanged(Entity<Scp096Component> ent, ref SleepStateChangedEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));
    }

    protected virtual void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var entityUid, out var targetComponent))
        {
            targetComponent.TargetedBy.Remove(ent.Owner);
            Dirty(entityUid, targetComponent);

            if (targetComponent.TargetedBy.Count == 0)
            {
                RemComp<Scp096TargetComponent>(entityUid);
            }
        }
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
        if (ent.Comp.InRageMode)
            args.Cancel();
    }

    private void OnCollide(Entity<Scp096Component> ent, ref StartCollideEvent args)
    {
        if (!TryComp<DoorComponent>(args.OtherEntity, out var doorComponent))
            return;

        HandleDoorCollision(ent, (args.OtherEntity, doorComponent));
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
    private void RefreshSpeedModifiers(Entity<Scp096Component> ent)
    {
        var newSpeed = ent.Comp.InRageMode ? ent.Comp.RageSpeed : ent.Comp.BaseSpeed;
        _speedModifier.ChangeBaseSpeed(ent, newSpeed, newSpeed, 20.0f);
    }

    /// <summary>
    /// Проверяет, может ли скромник ударить эту сущность.
    /// Можно ударить свою цель, структуру, борга или животного.
    /// </summary>
    private bool CanAttack(Entity<Scp096Component> scp, EntityUid target)
    {
        if (!scp.Comp.InRageMode)
            return false;

        // Если цель не имеет компонента моргания, значит это 99% не игрок
        // И скромник имеет право это расхуярить(это структура, борг или животное)
        if (!HasComp<BlinkableComponent>(target))
            return true;

        if (!TryComp<Scp096TargetComponent>(target, out var targetComp))
            return false;

        if (!targetComp.TargetedBy.Contains(scp))
            return false;

        return true;
    }

    #endregion

    #region Virtuals

    protected virtual void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity) {}
    protected virtual void OnHandleState(Entity<Scp096Component> ent, ref AfterAutoHandleStateEvent args) {}
    protected virtual void OnInit(Entity<Scp096Component> ent, ref ComponentInit args) {}

    #endregion
}

/// <summary>
/// Ивент, вызываемый при смене состояния скромника.
/// </summary>
/// <param name="inRage">Вошел или вышел из режима ярости</param>
public sealed class Scp096RageChangedEvent(bool inRage) : EntityEventArgs
{
    public readonly bool InRage = inRage;
}

/// <summary>
/// Ивент, информирующий клиент, что требуется перепроверять внешний вид scp-096.
/// </summary>
/// <param name="netEntity"><see cref="NetEntity"/> scp-096</param>
[Serializable, NetSerializable]
public sealed class Scp096RequireUpdateVisualsEvent(NetEntity netEntity) : EntityEventArgs
{
    public NetEntity NetEntity = netEntity;
}
