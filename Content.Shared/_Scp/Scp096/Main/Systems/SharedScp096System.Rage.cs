using Content.Shared._Scp.Mobs.Components;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Components;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;

    private void InitializeRage()
    {
        SubscribeLocalEvent<ActiveScp096HeatingUpComponent, ComponentStartup>(OnHeatingUpStart);
        SubscribeLocalEvent<ActiveScp096HeatingUpComponent, ComponentShutdown>(OnHeatingUpShutdown);

        SubscribeLocalEvent<ActiveScp096RageComponent, ComponentStartup>(OnRageStart);
        SubscribeLocalEvent<ActiveScp096RageComponent, ComponentShutdown>(OnRageShutdown);
    }

    #region Event handlers

    private void OnHeatingUpStart(Entity<ActiveScp096HeatingUpComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<Scp096Component>(ent, out var scp096))
        {
            Log.Error($"Found entity with {nameof(ActiveScp096HeatingUpComponent)} but without {nameof(Scp096Component)}: {ToPrettyString(ent)}, prototype: {Prototype(ent)}");
            return;
        }

        // Устанавливаем время окончания пред-агр состояния
        ent.Comp.RageHeatUpEnd = _timing.CurTime + scp096.RageHeatUp;

        // Устанавливаем звук пред-агр состояния
        _ambientSound.SetSound(ent, scp096.TriggerSound);
        _ambientSound.SetRange(ent, 30f);
        _ambientSound.SetVolume(ent, 20f);

        // Если скромник был застанен - убираем это
        _stamina.TryTakeStamina(ent, -100);
        _stun.TryUnstun(ent.Owner);

        // Заставляем трястись
        _jittering.AddJitter(ent, -10, 100);

        // Устанавливаем ограничения на взаимодействие
        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = false;
            restriction.CanBeDisarmed = false;
            restriction.CanStandingState = false;
            Dirty(ent.Owner, restriction);
        }

        // Устанавливаем ограничение на передвижение
        EnsureComp<BlockMovementComponent>(ent);
        EnsureComp<NoRotateOnInteractComponent>(ent);
        _actionBlocker.UpdateCanMove(ent);

        // TODO: Смена спрайта(ждем спрайтеров)

        Dirty(ent);
        Dirty(ent.Owner, scp096);
    }

    private void OnHeatingUpShutdown(Entity<ActiveScp096HeatingUpComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        // Сниманием тряску
        RemComp<JitteringComponent>(ent);

        // Убираем ограничения на взаимодействие
        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = true;
            restriction.CanBeDisarmed = true;
            restriction.CanStandingState = true;
            Dirty(ent.Owner, restriction);
        }

        // Убираем ограничение на передвижение
        RemComp<BlockMovementComponent>(ent);
        RemComp<NoRotateOnInteractComponent>(ent);
        _actionBlocker.UpdateCanMove(ent);
    }

    private void OnRageStart(Entity<ActiveScp096RageComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.RageStartTime = _timing.CurTime;
        Dirty(ent);

        // Устанавливаем ограничения на взаимодействие
        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = false;
            restriction.CanBeDisarmed = false;
            restriction.CanStandingState = false;
            Dirty(ent.Owner, restriction);
        }

        // Устанавливаем звук ора
        _ambientSound.SetSound(ent, ent.Comp.RageSound);
        _ambientSound.SetRange(ent, 20f);
        _ambientSound.SetVolume(ent, 10f);

        // Запрашиваем обновление внешнего вида
        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        // Обновляем скорость передвижения
        RefreshSpeedModifiers(ent.Owner, true);
    }

    private void OnRageShutdown(Entity<ActiveScp096RageComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<Scp096Component>(ent, out var scp096))
        {
            Log.Error($"Found entity with {nameof(ActiveScp096RageComponent)} but without {nameof(Scp096Component)}: {ToPrettyString(ent)}, prototype: {Prototype(ent)}");
            return;
        }

        // Возвращаем звук плача
        _ambientSound.SetSound(ent, scp096.CrySound);
        _ambientSound.SetRange(ent, 4f);
        _ambientSound.SetVolume(ent, -14f);

        // Усыпляем скромника
        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, scp096.PacifiedTime);

        // Убираем наложенные ограничения на взаимодействие
        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = true;
            restriction.CanBeDisarmed = true;
            restriction.CanStandingState = true;
            Dirty(ent.Owner, restriction);
        }

        // Запрашиваем обновление внешнего вида.
        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        // Обновляем скорость передвижения
        RefreshSpeedModifiers(ent.Owner, false);
    }

    #endregion

    private void UpdateHeatingUp()
    {
        var query = EntityQueryEnumerator<ActiveScp096HeatingUpComponent, Scp096Component>();
        while (query.MoveNext(out var uid, out var component, out _))
        {
            if (!component.RageHeatUpEnd.HasValue)
                continue;

            if (_timing.CurTime < component.RageHeatUpEnd.Value)
                continue;

            TryStartRage(uid);
        }
    }

    private void UpdateRage()
    {
        var query = EntityQueryEnumerator<ActiveScp096RageComponent, Scp096Component>();
        while (query.MoveNext(out var uid, out var rage, out var scp096))
        {
            if (!rage.RageStartTime.HasValue)
                continue;

            var elapsedTime = _timing.CurTime - rage.RageStartTime.Value;
            if (elapsedTime < scp096.RageDuration)
                continue;

            OnRageTimeExceeded((uid, scp096));
        }
    }

    /// <summary>
    /// Вызывается при окончании времени на поиск и уничтожение целей.
    /// Убирает все цели из списка целей.
    /// </summary>
    private void OnRageTimeExceeded(Entity<Scp096Component> ent)
    {
        RemoveAllTargets(ent);
    }

    /// <summary>
    /// Умиротворяет скромника.
    /// Происходит после того, как все цели были убиты и разорваны.
    /// </summary>
    private void Pacify(EntityUid uid)
    {
        if (!HasComp<ActiveScp096RageComponent>(uid))
        {
            Log.Error($"Trying to pacify SCP-096 while not being at rage - {ToPrettyString(uid)}");
            return;
        }

        RemComp<ActiveScp096RageComponent>(uid);
    }

    /// <summary>
    /// Переводит скромника в состояние ярости.
    /// </summary>
    private bool TryMakeAngry(EntityUid uid)
    {
        if (HasComp<ActiveScp096RageComponent>(uid) || HasComp<ActiveScp096HeatingUpComponent>(uid))
            return false;

        EnsureComp<ActiveScp096HeatingUpComponent>(uid);
        return true;
    }

    private bool TryStartRage(EntityUid uid)
    {
        if (HasComp<ActiveScp096RageComponent>(uid) || !HasComp<ActiveScp096HeatingUpComponent>(uid))
            return false;

        RemComp<ActiveScp096HeatingUpComponent>(uid);
        EnsureComp<ActiveScp096RageComponent>(uid);

        return true;
    }
}
