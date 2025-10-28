using Content.Shared._Scp.Mobs.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Stunnable;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly SharedStaminaSystem _stamina = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;

    private void UpdateRage()
    {
        var query = EntityQueryEnumerator<ActiveScp096HeatingUpComponent, Scp096Component>();

        while (query.MoveNext(out var uid, out var component, out var scp096))
        {
            if (!component.RageHeatUpEnd.HasValue)
                continue;

            if (_timing.CurTime < component.RageHeatUpEnd.Value)
                continue;

            Rage((uid, scp096));
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
    private void Pacify(Entity<Scp096Component> ent)
    {
        ent.Comp.InRageMode = false;
        ent.Comp.RageStartTime = null;
        Dirty(ent);

        _ambientSound.SetSound(ent, ent.Comp.CrySound);
        _ambientSound.SetRange(ent, 4f);
        _ambientSound.SetVolume(ent, -14f);
        _statusEffects.TryAddStatusEffectDuration(ent, StatusEffectSleep, ent.Comp.PacifiedTime);

        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = true;
            restriction.CanBeDisarmed = true;
            restriction.CanStandingState = true;
            Dirty(ent.Owner, restriction);
        }

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(false));
        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        RefreshSpeedModifiers(ent);
    }

    /// <summary>
    /// Переводит скромника в состояние ярости.
    /// </summary>
    private bool TryMakeAngry(Entity<Scp096Component> ent)
    {
        if (ent.Comp.InRageMode || HasComp<ActiveScp096HeatingUpComponent>(ent))
            return false;

        _ambientSound.SetSound(ent, ent.Comp.TriggerSound);
        _ambientSound.SetRange(ent, 30f);
        _ambientSound.SetVolume(ent, 20f);

        _stamina.TryTakeStamina(ent, -100);
        _stun.TryUnstun(ent.Owner);

        if (TryComp<ScpRestrictionComponent>(ent, out var restriction))
        {
            restriction.CanTakeStaminaDamage = false;
            restriction.CanBeDisarmed = false;
            restriction.CanStandingState = false;
            Dirty(ent.Owner, restriction);
        }

        EnsureComp<BlockMovementComponent>(ent);
        EnsureComp<NoRotateOnInteractComponent>(ent);
        var comp = EnsureComp<ActiveScp096HeatingUpComponent>(ent);
        comp.RageHeatUpEnd = _timing.CurTime + ent.Comp.RageHeatUp;

        // TODO: Смена спрайта(ждем спрайтеров)

        _actionBlocker.UpdateCanMove(ent);

        Dirty(ent);
        Dirty(ent.Owner, comp);

        return true;
    }

    private void Rage(Entity<Scp096Component> ent)
    {
        RemComp<BlockMovementComponent>(ent);
        RemComp<NoRotateOnInteractComponent>(ent);
        RemComp<ActiveScp096HeatingUpComponent>(ent);

        _actionBlocker.UpdateCanMove(ent);

        ent.Comp.InRageMode = true;
        ent.Comp.RageStartTime = _timing.CurTime;
        Dirty(ent);

        RaiseLocalEvent(ent, new Scp096RageChangedEvent(true));
        RaiseNetworkEvent(new Scp096RequireUpdateVisualsEvent(GetNetEntity(ent)));

        _ambientSound.SetSound(ent, ent.Comp.RageSound);
        _ambientSound.SetRange(ent, 20f);
        _ambientSound.SetVolume(ent, 10f);

        RefreshSpeedModifiers(ent);
    }
}
