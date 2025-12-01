using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedJitteringSystem _jittering = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public static readonly ProtoId<AmbientMusicPrototype> RageAmbience = "Scp096Rage";

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
        // Устанавливаем время окончания пред-агр состояния
        ent.Comp.RageHeatUpEnd = _timing.CurTime + ent.Comp.RageHeatUp;

        // Устанавливаем звук пред-агр состояния
        UpdateAudio(ent.Owner, ent.Comp.TriggerSound);

        if (_net.IsServer)
            RaiseNetworkEvent(new NetworkAmbientMusicEvent(RageAmbience), ent);

        // Если скромник был застанен или сидит - убираем это
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

        ToggleMovement(ent, false);
        UpdateAppearance(ent.Owner);
        TryToggleTears(ent.Owner, false);

        Dirty(ent);
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
        ToggleMovement(ent, true);
    }

    protected virtual void OnRageStart(Entity<ActiveScp096RageComponent> ent, ref ComponentStartup args)
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
        UpdateAudio(ent.Owner, ent.Comp.RageSound);

        // Запрашиваем обновление внешнего вида
        UpdateAppearance(ent.Owner);

        // Обновляем скорость передвижения
        RefreshSpeedModifiers(ent.Owner);
    }

    protected virtual void OnRageShutdown(Entity<ActiveScp096RageComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        if (!TryComp<Scp096Component>(ent, out var scp096))
        {
            Log.Error($"Found entity with {nameof(ActiveScp096RageComponent)} but without {nameof(Scp096Component)}: {ToPrettyString(ent)}, prototype: {Prototype(ent)}");
            return;
        }

        // Возвращаем звук плача
        UpdateAudio(ent.Owner, scp096.CrySound);

        scp096.TargetsCount = 0;
        Dirty(ent, scp096);

        if (_net.IsServer)
            RaiseNetworkEvent(new NetworkAmbientMusicEventStop(), ent);

        // Усыпляем скромника
        if (!_statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, ent.Comp.PacifiedTime))
        {
            // При усыплении скромника и так меняется внешний вид, нет смысла делать это несколько раз.
            // Поэтому запрашиваем обновление внешнего вида только при неуспешном усыплении
            UpdateAppearance(ent.Owner);
        }

        // Убираем наложенные ограничения на взаимодействие
        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = true;
            restriction.CanBeDisarmed = true;
            restriction.CanStandingState = true;
            Dirty(ent.Owner, restriction);
        }

        // Обновляем скорость передвижения
        RefreshSpeedModifiers(ent.Owner, true);
        TryToggleTears(ent.Owner, true);
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
        var query = EntityQueryEnumerator<ActiveScp096RageComponent>();
        while (query.MoveNext(out var uid, out var rage))
        {
            if (!rage.RageStartTime.HasValue)
                continue;

            var elapsedTime = _timing.CurTime - rage.RageStartTime.Value;
            if (elapsedTime < rage.RageDuration)
                continue;

            RemoveAllTargets();

            // Возможная заглушка на случай, если удаление всех сущностей произойдет НЕ в 1 тик.
            // Что приведет к проблемам.
            rage.RageStartTime = null;
            Dirty(uid, rage);
        }
    }

    /// <summary>
    /// Переводит скромника в состояние ярости.
    /// </summary>
    private bool TryMakeAngry(EntityUid uid)
    {
        if (HasComp<ActiveScp096RageComponent>(uid) || HasComp<ActiveScp096WithoutFaceComponent>(uid))
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
