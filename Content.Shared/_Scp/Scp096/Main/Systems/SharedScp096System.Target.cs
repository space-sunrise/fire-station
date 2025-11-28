using System.Linq;
using Content.Shared._Scp.Audio;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Protection;
using Content.Shared._Scp.Watching;
using Content.Shared._Scp.Watching.FOV;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Audio;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly FieldOfViewSystem _fov = default!;
    [Dependency] private readonly EyeWatchingSystem _watching = default!;
    [Dependency] private readonly INetManager _net = default!;

    private static readonly ProtoId<AmbientMusicPrototype> TargetAmbience = "Scp096Target";

    private void InitializeTargets()
    {
        SubscribeLocalEvent<Scp096TargetComponent, DamageChangedEvent>(OnTargetDamageChanged);
        SubscribeLocalEvent<Scp096TargetComponent, MobStateChangedEvent>(OnTargetMobStateChanged);
        SubscribeLocalEvent<Scp096TargetComponent, ComponentStartup>(OnTargetStartup);
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

    private void OnTargetStartup(Entity<Scp096TargetComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsServer)
            RaiseNetworkEvent(new NetworkAmbientMusicEvent(TargetAmbience), ent);
    }

    private void OnTargetShutdown(Entity<Scp096TargetComponent> ent, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        var query = EntityQueryEnumerator<ActiveScp096RageComponent, Scp096Component>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            RemoveTarget(uid, ent.AsNullable(), false);
        }

        if (_net.IsServer)
            RaiseNetworkEvent(new NetworkAmbientMusicEventStop(), ent);
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
        TryMakeAngry(scp);
        EnsureComp<Scp096TargetComponent>(target);

        if (_net.IsServer)
            _audio.PlayGlobal(scp.Comp.SeenSound, target);
    }

    /// <summary>
    /// Убирает конкретную цель из списка целей scp-096
    /// </summary>
    protected virtual void RemoveTarget(Entity<ActiveScp096RageComponent?> scp, EntityUid target, bool removeComponent = true)
    {
        if (!Resolve(scp, ref scp.Comp))
            return;

        if (removeComponent)
            RemComp<Scp096TargetComponent>(target);

        if (!HasAnyTargets())
            Pacify(scp);
    }

    /// <summary>
    /// Убирает все текущие цели у scp-096
    /// </summary>
    private void RemoveAllTargets(EntityUid ent)
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();

        while (query.MoveNext(out var target, out _))
        {
            RemoveTarget(ent, target);
        }
    }

    /// <summary>
    /// Проверяет, может ли цель быть целью скромника.
    /// Включает в себя различные проверки на поле зрения, защиту и прочее.
    /// </summary>
    private bool IsValidTarget(Entity<Scp096Component> scp, EntityUid target, bool ignoreAngle = false)
    {
        // Уже является целью?
        if (HasComp<Scp096TargetComponent>(target))
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
        if (!_fov.IsInViewAngle(scp.Owner, viewer, scp.Comp.ArgoAngle))
            return false;

        // Проверяем, смотри ли цель в лицо 096
        if (!_fov.IsInViewAngle(viewer, scp.Owner, scp.Comp.ArgoAngle))
            return false;

        // Соответственно если обе проверки прошли, то цель видит 096
        return true;
    }

    public List<Entity<Scp096TargetComponent>> GetTargets()
    {
        return _helpers.GetAll<Scp096TargetComponent>().ToList();
    }

    public bool HasAnyTargets()
    {
        var query = EntityQueryEnumerator<Scp096TargetComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (TerminatingOrDeleted(uid))
                continue;

            if (comp.LifeStage != ComponentLifeStage.Running)
                continue;

            // Значит нашли хотя бы одного
            return true;
        }

        return false;
    }
}
