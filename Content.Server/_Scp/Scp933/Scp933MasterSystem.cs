using Content.Shared._Scp.Scp933;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Localization;

namespace Content.Server._Scp.Scp933;

public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp933MasterComponent, ComponentStartup>(OnMasterStartup);
        SubscribeLocalEvent<Scp933MasterComponent, ComponentShutdown>(OnMasterShutdown);
        SubscribeLocalEvent<Scp933FaceTornComponent, ComponentShutdown>(OnFaceTornShutdown);
        SubscribeLocalEvent<Scp933FaceTornComponent, MobStateChangedEvent>(OnFaceTornMobStateChanged);
    }

    public bool HasAnyScp933Host()
    {
        var query = EntityQueryEnumerator<Scp933MasterComponent>();
        return query.MoveNext(out _, out _);
    }

    private void OnMasterStartup(Entity<Scp933MasterComponent> ent, ref ComponentStartup args)
    {
        EraseFaceFor933(ent);

        _popup.PopupEntity(Loc.GetString("scp933-host-emerged"), ent, ent, PopupType.LargeCaution);
    }

    private void OnMasterShutdown(Entity<Scp933MasterComponent> ent, ref ComponentShutdown args)
    {
        foreach (var victim in ent.Comp.FaceTornVictims)
            RemComp<Scp933FaceTornComponent>(victim);
    }

    private void OnFaceTornShutdown(Entity<Scp933FaceTornComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.MutedByScp933)
            RemComp<MutedComponent>(ent);

        if (ent.Comp.TornBy is not { } bearer)
            return;

        if (!TryComp<Scp933MasterComponent>(bearer, out var master))
            return;

        if (!master.FaceTornVictims.Remove(ent))
            return;

        Dirty(bearer, master);
    }

    private void OnFaceTornMobStateChanged(Entity<Scp933FaceTornComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        RemComp<Scp933FaceTornComponent>(ent);
    }

    public void ApplyHostBuffs(EntityUid uid)
    {
        if (!TryComp<Scp933MasterComponent>(uid, out var master))
            return;

        var ritualSettings = EnsureComp<Scp933RitualSettingsComponent>(uid);

        // Хост не чувствует страха - у него нет разума, только инстинкты ленты
        RemCompDeferred<FearComponent>(uid);

        if (ritualSettings.HealHostOnEmerge)
        {
            if (HasComp<DamageableComponent>(uid))
                _damageable.SetAllDamage(uid, FixedPoint2.Zero);
        }

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        _mobThresholds.SetAllowRevives(uid, ritualSettings.AllowRevives, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, ritualSettings.HostHealthThresholds.Alive, MobState.Alive, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, ritualSettings.HostHealthThresholds.Critical, MobState.Critical, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, ritualSettings.HostHealthThresholds.Dead, MobState.Dead, thresholds);
        _mobThresholds.VerifyThresholds(uid, thresholds);

        if (TryComp<MeleeWeaponComponent>(uid, out var melee))
        {
            var damage = ritualSettings.MeleeSettings;
            melee.Damage = new() { DamageDict = { [damage.DamageType.Id] = damage.DamageAmount } };
            melee.Range = damage.Range;
            melee.Angle = damage.Angle;
            Dirty(uid, melee);
        }
    }

    /// <summary>
    /// Выдать носителю ленты после инкубации.
    /// </summary>
    public void ConvertToMaster(EntityUid victim)
    {
        if (HasComp<Scp933MasterComponent>(victim))
            return;

        EnsureComp<Scp933MasterComponent>(victim);
    }

    /// <summary>
    /// Финал ритуала: срыв ленты с лица — визуал без лица и немота.
    /// </summary>
    public void ApplyFaceTornAfterRip(EntityUid tapeBearer, EntityUid victim)
    {
        if (!TryComp<Scp933MasterComponent>(tapeBearer, out var masterComp))
            return;

        if (!TryComp<Scp933PossibleTargetComponent>(victim, out var targetComp) || !targetComp.CanBeFaceTorn)
            return;

        if (HasComp<Scp933FaceTornComponent>(victim) || HasComp<Scp933MasterComponent>(victim))
            return;

        var torn = EnsureComp<Scp933FaceTornComponent>(victim);
        torn.TornBy = tapeBearer;
        torn.MutedByScp933 = true;
        Dirty(victim, torn);
        EnsureComp<MutedComponent>(victim);
        TryComp<Scp933VisualEffectsComponent>(victim, out var victimVisuals);
        EraseFaceFor933(victim, victimVisuals);

        masterComp.FaceTornVictims.Add(victim);
        Dirty(tapeBearer, masterComp);

        // Лечим жертву если нужно
        TryHealVictim(tapeBearer, victim);

        _popup.PopupEntity(Loc.GetString("scp933-victim-face-torn"), victim, victim, PopupType.LargeCaution);
    }

    /// <summary>
    /// Лечит жертву после срыва ленты если включено HealVictimsOnHostEmerge.
    /// </summary>
    protected override void TryHealVictim(EntityUid tapeBearer, EntityUid victim)
    {
        if (!TryComp<Scp933RitualSettingsComponent>(tapeBearer, out var ritualSettings))
            return;

        if (!ritualSettings.HealVictimsOnHostEmerge)
            return;

        if (HasComp<DamageableComponent>(victim))
            _damageable.SetAllDamage(victim, FixedPoint2.Zero);
    }
}
