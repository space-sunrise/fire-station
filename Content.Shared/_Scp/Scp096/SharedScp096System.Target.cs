using Content.Shared._Scp.Scp096.Protection;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Network;

namespace Content.Shared._Scp.Scp096;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

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

    /// <summary>
    /// Проверяет, может ли цель быть целью scp-096.
    /// Если может - добавляет ее в список целей. Возвращает полученный результат
    /// </summary>
    public bool TryAddTarget(Entity<Scp096Component> scp, EntityUid target, bool ignoreAngle = false, bool ignoreMask = false)
    {
        if (!CanBeAggro(scp, ignoreMask))
            return false;

        if (!IsValidTarget(scp, target, ignoreAngle))
            return false;

        AddTarget(scp, target);

        return true;
    }

    /// <summary>
    /// Добавляет конкретную цель в список целей scp-096.
    /// Не имеет в себе проверок, для проверок использовать <seealso cref="TryAddTarget"/>
    /// </summary>
    protected virtual void AddTarget(Entity<Scp096Component> scp, EntityUid target)
    {
        scp.Comp.Targets.Add(target);

        var scpTarget = EnsureComp<Scp096TargetComponent>(target);
        scpTarget.TargetedBy.Add(scp);

        Dirty(target, scpTarget);
        Dirty(scp);

        if (_net.IsServer)
            _audio.PlayGlobal(scp.Comp.SeenSound, target);

        TryMakeAngry(scp);
    }

    /// <summary>
    /// Убирает конкретную цель из списка целей scp-096
    /// </summary>
    protected virtual void RemoveTarget(Entity<Scp096Component> scp, Entity<Scp096TargetComponent?> target, bool removeComponent = true)
    {
        if (!Resolve(target, ref target.Comp))
            return;

        scp.Comp.Targets.Remove(target);
        target.Comp.TargetedBy.Remove(scp);

        Dirty(target);
        Dirty(scp);

        if (target.Comp.TargetedBy.Count == 0 && removeComponent)
            RemComp<Scp096TargetComponent>(target);

        if (scp.Comp.Targets.Count == 0)
            Pacify(scp);
    }

    /// <summary>
    /// Убирает все текущие цели у scp-096
    /// </summary>
    private void RemoveAllTargets(Entity<Scp096Component> ent)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var targetUid, out _))
        {
            RemoveTarget(ent, targetUid);
        }
    }

    /// <summary>
    /// Проверяет, может ли цель быть целью скромника.
    /// Включает в себя различные проверки на поле зрения, защиту и прочее.
    /// </summary>
    private bool IsValidTarget(Entity<Scp096Component> scp, EntityUid target, bool ignoreAngle = false)
    {
        if (scp.Comp.Targets.Contains(target))
            return false;

        // Проверяем, может ли цель видеть 096. Без учета поля зрения
        if (!_watching.IsWatchedBy(scp, [target], viewers: out _, false))
            return false;

        // Проверяем, есть ли у цели защита от 096
        if (TryComp<Scp096ProtectionComponent>(target, out var protection) && !_random.ProbForEntity(scp, protection.ProblemChance))
            return false;

        // Проверяем, смотрит ли 096 на цель и цель на 096
        if (!IsTargetSeeScp096(target, scp, ignoreAngle))
            return false;

        // Если все условия выполнены, то цель валидна
        return true;
    }

    /// <summary>
    /// Проверяет, видит ли цель SCP-096.
    /// Использует особые проверки поля зрения для проверки "смотрят ли они друг-другу в лицо"
    /// </summary>
    private bool IsTargetSeeScp096(EntityUid viewer, Entity<Scp096Component> scp, bool ignoreAngle)
    {
        // Если игнорируем угол, то считаем, что смотрящий видит 096
        if (ignoreAngle)
            return true;

        // Проверяем, смотрит ли 096 в лицо цели
        if (!_fov.IsInViewAngle(scp.Owner, scp.Comp.ArgoAngle, Angle.Zero, viewer))
            return false;

        // Проверяем, смотри ли цель в лицо 096
        if (!_fov.IsInViewAngle(viewer, scp.Comp.ArgoAngle, Angle.Zero, scp.Owner))
            return false;

        // Соответственно если обе проверки прошли, то цель видит 096
        return true;
    }
}
