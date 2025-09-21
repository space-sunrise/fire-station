using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Popups;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    private void InitTargets()
    {
        SubscribeLocalEvent<Scp096TargetComponent, DamageChangedEvent>(OnTargetDamageChanged);
        SubscribeLocalEvent<Scp096TargetComponent, MobStateChangedEvent>(OnTargetMobStateChanged);
        SubscribeLocalEvent<Scp096TargetComponent, ComponentShutdown>(OnTargetShutdown);
    }

    private void OnTargetDamageChanged(Entity<Scp096TargetComponent> ent, ref DamageChangedEvent args)
    {
        if (!args.DamageIncreased)
            return;

        if (!HasComp<Scp096Component>(args.Origin))
            return;

        ent.Comp.AlreadyAppliedDamage += args.DamageDelta?.GetTotal() ?? FixedPoint2.Zero;
        Dirty(ent);

        // Убираем цель только после нанесения суммарно нужного количества урона
        if (ent.Comp.AlreadyAppliedDamage >= ent.Comp.TotalDamageToStop)
            RemComp<Scp096TargetComponent>(ent);
    }

    private void OnTargetMobStateChanged(Entity<Scp096TargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_mobState.IsDead(ent))
            return;

        if (!HasComp<Scp096Component>(args.Origin))
            return;

        // Сообщаем игроку, что нужно продолжать разрывать цель.
        _popup.PopupClient(Loc.GetString("scp096-keep-attacking"), ent, args.Origin, PopupType.Medium);
    }

    private void OnTargetShutdown(Entity<Scp096TargetComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var scpEntityUid, out var scp096Component))
        {
            RemoveTarget((scpEntityUid, scp096Component), ent.AsNullable(), false);
        }
    }
}
