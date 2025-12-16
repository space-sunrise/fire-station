using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Containment.Cage;
using Content.Shared._Scp.Watching;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Storage.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp173;

public abstract class SharedScp173System : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly EyeWatchingSystem Watching = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    protected static readonly TimeSpan ReagentCheckInterval = TimeSpan.FromSeconds(1);

    public const float ContainmentRoomSearchRadius = 8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, AttackAttemptEvent>((uid, _, args) =>
        {
            if (Watching.IsWatched(uid))
                args.Cancel();
        });

        SubscribeLocalEvent<Scp173Component, ChangeDirectionAttemptEvent>(OnDirectionAttempt);
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<Scp173Component, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<Scp173Component, MoveEvent>(OnMove);

        SubscribeLocalEvent<Scp173Component, Scp173BlindAction>(OnStartedBlind);
        SubscribeLocalEvent<Scp173Component, Scp173StartBlind>(OnBlind);
    }

    #region Movement

    private void OnDirectionAttempt(Entity<Scp173Component> ent, ref ChangeDirectionAttemptEvent args)
    {
        if (Watching.IsWatched(ent.Owner) && !IsInScpCage(ent, out _))
            args.Cancel();
    }

    private void OnMoveAttempt(Entity<Scp173Component> ent, ref UpdateCanMoveEvent args)
    {
        if (Watching.IsWatched(ent.Owner) && !IsInScpCage(ent, out _))
            args.Cancel();
    }

    private void OnMoveInput(Entity<Scp173Component> ent, ref MoveInputEvent args)
    {
        // Метод подвязанный на MoveInputEvent так же нужен, вместе с методом на MoveEvent
        // Этот метод исправляет проблему, когда 173 должен мочь двинуться, но ему об этом никто не сказал
        // То есть последний вопрос от 173 МОГУ ЛИ Я ДВИНУТЬСЯ был когда он еще мог двинуться, через MoveEvent
        // Потом он перестал мочь, и следственно больше НЕ МОЖЕТ задать вопрос, может они двинуться
        // Это фикслось в игре сменой направления спрайта мышкой
        // Но данный метод как раз будет спрашивать у 173, может ли он сдвинуться, когда как раз не двигается
        _blocker.UpdateCanMove(ent);
    }

    private void OnMove(Entity<Scp173Component> ent, ref MoveEvent args)
    {
        _blocker.UpdateCanMove(ent);
    }

    #endregion

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

    protected virtual void BreakNeck(EntityUid target, Scp173Component scp) {}

    #endregion

    #region Public API

    public void BlindEveryoneInRange(EntityUid scp, TimeSpan time, bool predicted = true)
    {
        var eyes = Watching.GetWatchers(scp);

        foreach (var eye in eyes)
        {
            _blinking.ForceBlind(eye, time, predicted);
        }

        // TODO: Add sound.
    }

    /// <summary>
    /// Находится ли 173 в контейнере для перевозки
    /// </summary>
    public bool IsInScpCage(EntityUid uid, [NotNullWhen(true)] out EntityUid? storage)
    {
        storage = null;

        if (TryComp<InsideEntityStorageComponent>(uid, out var insideEntityStorageComponent) &&
            HasComp<ScpCageComponent>(insideEntityStorageComponent.Storage))
        {
            storage = insideEntityStorageComponent.Storage;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Находится ли 173 в своей камере. Проверяется по наличию рядом спавнера работы
    /// </summary>
    /// TODO: Оптимизировать, использовав EntityQueryEnumerator и ранний выход
    public bool IsContained(EntityUid uid)
    {
        return _lookup.GetEntitiesInRange<Scp173BlockStructureDamageComponent>(Transform(uid).Coordinates, ContainmentRoomSearchRadius)
            .Any(entity => _interaction.InRangeUnobstructed(uid, entity.Owner, ContainmentRoomSearchRadius));
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

        if (!IsWatched(uid, out var watchers))
        {
            if (showPopups)
                _popup.PopupClient(Loc.GetString("scp173-blind-failed-too-few-watchers"), uid, uid);

            return false;
        }

        if (watchers.Count <= 3)
        {
            if (showPopups)
                _popup.PopupClient(Loc.GetString("scp173-blind-failed-too-few-watchers"), uid, uid);

            return false;
        }

        return true;
    }

    public bool IsWatched(EntityUid target, out HashSet<EntityUid> viewers)
    {
        var watchers = Watching.GetWatchers(target);

        viewers = watchers
            .Where(eye => Watching.CanBeWatched(eye, target))
            .ToHashSet();

        return viewers.Count != 0;
    }

    #endregion
}
