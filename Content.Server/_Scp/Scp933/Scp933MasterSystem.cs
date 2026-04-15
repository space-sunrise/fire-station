using Content.Shared._Scp.Scp933;
using Content.Shared.Damage.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Weapons.Melee;

namespace Content.Server._Scp.Scp933;

public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    public bool HasAnyScp933Host()
    {
        var query = EntityQueryEnumerator<Scp933MasterComponent>();
        return query.MoveNext(out _, out _);
    }

    public void ApplyHostBuffs(EntityUid uid)
    {
        if (!TryComp<Scp933MasterComponent>(uid, out var master))
            return;

        if (HasComp<DamageableComponent>(uid))
            _damageable.SetAllDamage(uid, FixedPoint2.Zero);

        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        // Target can be dead before ritual completes; explicitly allow revive transition.
        _mobThresholds.SetAllowRevives(uid, master.AllowRevivesOnHost, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, FixedPoint2.Zero, MobState.Alive, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, master.CriticalThreshold, MobState.Critical, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, master.DeadThreshold, MobState.Dead, thresholds);
        _mobThresholds.VerifyThresholds(uid, thresholds);

        if (TryComp<MeleeWeaponComponent>(uid, out var melee))
        {
            melee.Damage = new DamageSpecifier { DamageDict = { ["Blunt"] = master.HostBluntDamage } };
            Dirty(uid, melee);
        }
    }

}
