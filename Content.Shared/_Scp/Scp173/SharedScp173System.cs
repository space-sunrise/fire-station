﻿using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Containment.Cage;
using Content.Shared._Scp.Watching;
using Content.Shared.ActionBlocker;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Content.Shared.Storage.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp173;

// TODO: Создать единую систему/метод для получения всех смотрящих на какого-либо ентити
// Где будет учитываться, закрыты ли глаза, не моргает ли человек т.п. базовая информация

public abstract class SharedScp173System : EntitySystem
{
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly ActionBlockerSystem _blocker = default!;
    [Dependency] protected readonly EyeWatchingSystem Watching = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public const float ContainmentRoomSearchRadius = 8f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, ComponentInit>(OnInit);

        #region Blocker

        SubscribeLocalEvent<Scp173Component, AttackAttemptEvent>((uid, _, args) =>
        {
            if (Watching.IsWatched(uid))
                args.Cancel();
        });

        #endregion

        #region Movement

        SubscribeLocalEvent<Scp173Component, ChangeDirectionAttemptEvent>(OnDirectionAttempt);
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<Scp173Component, MoveInputEvent>(OnMoveInput);
        SubscribeLocalEvent<Scp173Component, MoveEvent>(OnMove);

        #endregion

        #region Abillities

        SubscribeLocalEvent<Scp173Component, StartCollideEvent>(OnCollide);

        SubscribeLocalEvent<Scp173Component, Scp173BlindAction>(OnBlind);

        #endregion
    }

    private void OnInit(Entity<Scp173Component> ent, ref ComponentInit args)
    {
        // Fallback
        ent.Comp.NeckSnapDamage ??= new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Blunt"), 200);

        Dirty(ent);
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

    private void OnCollide(Entity<Scp173Component> ent, ref StartCollideEvent args)
    {
        var target = args.OtherEntity;

        if (!TryComp<PhysicsComponent>(ent, out var physicsComponent))
            return;

        // Мы должны двигаться, чтобы сломать шею
        if (physicsComponent.LinearVelocity.IsLengthZero())
            return;

        BreakNeck(target, ent.Comp);
    }

    private void OnBlind(Entity<Scp173Component> ent, ref Scp173BlindAction args)
    {
        if (args.Handled)
            return;

        BlindEveryoneInRange(ent);

        args.Handled = true;
    }

    protected abstract void BreakNeck(EntityUid target, Scp173Component scp);

    #endregion

    #region Public API

    public void BlindEveryoneInRange(EntityUid scp, float range = 16f, float time = 6f)
    {
        var eyes = Watching.GetWatchers(scp);

        foreach (var eye in eyes)
        {
            _blinking.ForceBlind(eye, TimeSpan.FromSeconds(time));
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
    public bool IsContained(EntityUid uid)
    {
        return _lookup.GetEntitiesInRange(uid, ContainmentRoomSearchRadius)
            .Any(entity => HasComp<Scp173BlockStructureDamageComponent>(entity) &&
                           _interaction.InRangeUnobstructed(uid, entity, ContainmentRoomSearchRadius));
    }

    #endregion
}
