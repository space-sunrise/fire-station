using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Containment.Cage;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Watching;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp173;

// TODO: Выделить логику блокировки движения при смотрении в отдельную систему со своим компонентом.
public abstract class SharedScp173System : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly EyeWatchingSystem Watching = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected static readonly TimeSpan ReagentCheckInterval = TimeSpan.FromSeconds(1f);

    public const float ContainmentRoomSearchRadius = 8f;

    private EntityQuery<InsideEntityStorageComponent> _insideQuery;
    private EntityQuery<ScpCageComponent> _scpCageQuery;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, Scp173BlindAction>(OnStartedBlind);
        SubscribeLocalEvent<Scp173Component, Scp173StartBlind>(OnBlind);

        _insideQuery = GetEntityQuery<InsideEntityStorageComponent>();
        _scpCageQuery = GetEntityQuery<ScpCageComponent>();
    }

    #region Abillities

    private void OnStartedBlind(Entity<Scp173Component> ent, ref Scp173BlindAction args)
    {
        if (args.Handled)
            return;

        if (!CanBlind(ent))
            return;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.Performer, ent.Comp.StartBlindTime, new Scp173StartBlind(), args.Performer)
        {
            Hidden = true,
            RequireCanInteract = false,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnBlind(Entity<Scp173Component> ent, ref Scp173StartBlind args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!CanBlind(ent))
            return;

        // По причине акшена это не предиктится.
        // Активация акшена у игрока не предугадывается другими игроками. Параша
        BlindEveryoneInRange(ent, ent.Comp.BlindnessTime, false);
        args.Handled = true;
    }

    #endregion

    #region Public API

    public void BlindEveryoneInRange(EntityUid scp, TimeSpan time, bool predicted = true)
    {
        using var blinkableList = ListPoolEntity<BlinkableComponent>.Rent();
        if (!Watching.TryGetAllEntitiesVisibleTo(scp, blinkableList.Value, flags: LookupFlags.Dynamic | LookupFlags.Approximate))
            return;

        foreach (var eye in blinkableList.Value)
        {
            _blinking.ForceBlind(eye.AsNullable(), time, predicted);
        }

        // TODO: Add sound.
    }

    /// <summary>
    /// Находится ли 173 в контейнере для перевозки
    /// </summary>
    public bool IsInScpCage(EntityUid uid, [NotNullWhen(true)] out EntityUid? storage)
    {
        storage = null;

        if (_insideQuery.TryComp(uid, out var insideEntityStorageComponent) &&
            _scpCageQuery.HasComp(insideEntityStorageComponent.Storage))
        {
            storage = insideEntityStorageComponent.Storage;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Находится ли 173 в своей камере. Проверяется по наличию рядом спавнера работы
    /// </summary>
    public bool IsContained(EntityUid uid)
    {
        return Watching.TryGetAnyEntitiesVisibleTo<Scp173BlockStructureDamageComponent>(uid,
            LineOfSightBlockerLevel.None,
            LookupFlags.Sensors | LookupFlags.Sundries,
            ContainmentRoomSearchRadius);
    }

    private bool CanBlind(EntityUid uid, bool showPopups = true)
    {
        if (!IsContained(uid))
        {
            if (showPopups)
                _popup.PopupClient(Loc.GetString("scp173-blind-failed-not-in-chamber"), uid, uid);

            return false;
        }

        if (IsInScpCage(uid, out var cage))
        {
            if (showPopups)
                _popup.PopupClient(Loc.GetString("scp-cage-suppress-ability", ("container", Name(cage.Value))), uid, uid);

            return false;
        }

        if (!Watching.TryGetWatchers(uid, out var watchers))
        {
            if (showPopups)
                _popup.PopupClient(Loc.GetString("scp173-blind-failed-too-few-watchers"), uid, uid);

            return false;
        }

        if (watchers <= 3)
        {
            if (showPopups)
                _popup.PopupClient(Loc.GetString("scp173-blind-failed-too-few-watchers"), uid, uid);

            return false;
        }

        return true;
    }

    #endregion

    #region Jump Helpers

    /// <summary>
    /// Проверяет, является ли сущность непроходимым препятствием.
    /// Используется для определения, нужно ли остановить прыжок SCP-173 при столкновении.
    /// </summary>
    protected bool IsImpassableObstacle(EntityUid entity)
    {
        if (!TryComp<PhysicsComponent>(entity, out var physics))
            return false;

        if (!physics.Hard)
            return false;

        var layer = (CollisionGroup) physics.CollisionLayer;

        return layer.HasFlag(CollisionGroup.WallLayer)
               || layer.HasFlag(CollisionGroup.GlassAirlockLayer)
               || layer.HasFlag(CollisionGroup.HumanoidBlockLayer);
    }

    /// <summary>
    /// Ограничивает координаты цели прыжка до максимальной дальности.
    /// Если цель дальше <paramref name="maxRange"/>, координаты обрезаются.
    /// </summary>
    protected static void ClampTargetToRange(MapCoordinates performerCoords, ref MapCoordinates targetCoords, float maxRange)
    {
        var direction = targetCoords.Position - performerCoords.Position;
        var distance = direction.Length();

        if (distance > maxRange)
        {
            direction = Vector2.Normalize(direction) * maxRange;
            targetCoords = performerCoords.Offset(direction);
        }
    }

    #endregion
}
