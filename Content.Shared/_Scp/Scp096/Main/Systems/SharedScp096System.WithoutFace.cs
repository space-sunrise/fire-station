using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.IdentityManagement;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Tag;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private void InitializeWithoutFace()
    {
        SubscribeLocalEvent<ActiveScp096WithoutFaceComponent, ComponentStartup>(OnWithoutFaceStartup);
        SubscribeLocalEvent<ActiveScp096WithoutFaceComponent, ComponentShutdown>(OnWithoutFaceShutdown);
        SubscribeLocalEvent<Scp096FaceComponent, DamageChangedEvent>(OnFaceDamageChanged);

        SubscribeLocalEvent<Scp096Component, HealingRelayEvent>(OnHealingRelay);
    }

    /// <summary>
    /// Переводит скромника в состояние содранного лица, выдавая нужные модификаторы и компоненты.
    /// </summary>
    private void OnWithoutFaceStartup(Entity<ActiveScp096WithoutFaceComponent> ent, ref ComponentStartup args)
    {
        var message = Loc.GetString("scp096-face-skin-rip-full", ("name", Identity.Name(ent, EntityManager)));
        _popup.PopupPredicted(message, ent, ent);

        _audio.PlayPredicted(ent.Comp.StartSound, ent, ent);
        UpdateAudio(ent.Owner, ent.Comp.AmbientSound);

        RefreshSpeedModifiers(ent.Owner);

        // Устанавливаем параметры атаки для режима содранного лица
        if (TryComp<MeleeWeaponComponent>(ent, out var melee))
        {
            ent.Comp.CachedDamage = melee.Damage;
            ent.Comp.CachedAttackRate = melee.AttackRate;
            ent.Comp.CachedStaminaDamageFactor = melee.BluntStaminaDamageFactor;

            melee.AttackRate = ent.Comp.AttackRate;
            melee.Damage = ent.Comp.Damage;
            melee.BluntStaminaDamageFactor = ent.Comp.StaminaDamageFactor;
            Dirty(ent, melee);
        }

        if (TryComp<MeleeThrowOnHitComponent>(ent, out var throwOnHit))
        {
            ent.Comp.CachedThrowSpeed = throwOnHit.Speed;
            ent.Comp.CachedThrowDistance = throwOnHit.Distance;

            throwOnHit.Speed = ent.Comp.ThrowSpeed;
            throwOnHit.Distance = ent.Comp.ThrowDistance;
            Dirty(ent, throwOnHit);
        }

        Dirty(ent);
        _tag.AddTags(ent, ent.Comp.TagsToAdd);
    }

    /// <summary>
    /// Возвращает скромника в стандартное состояние из состояния содранного лица
    /// </summary>
    private void OnWithoutFaceShutdown(Entity<ActiveScp096WithoutFaceComponent> ent, ref ComponentShutdown args)
    {
        var message = Loc.GetString("scp096-face-healed", ("name", Identity.Name(ent, EntityManager)));
        _popup.PopupPredicted(message, ent, ent);

        _audio.PlayPredicted(ent.Comp.ShutdownSound, ent, ent);
        UpdateAudio(ent.Owner);

        RefreshSpeedModifiers(ent.Owner, true);
        TryHealFace(ent);

        // Возвращаем стандартные параметры атаки
        if (TryComp<MeleeWeaponComponent>(ent, out var melee))
        {
            if (ent.Comp.CachedAttackRate != null)
                melee.AttackRate = ent.Comp.CachedAttackRate.Value;

            if (ent.Comp.CachedDamage != null)
                melee.Damage = ent.Comp.CachedDamage;

            if (ent.Comp.CachedStaminaDamageFactor != null)
                melee.BluntStaminaDamageFactor = ent.Comp.CachedStaminaDamageFactor.Value;

            Dirty(ent, melee);
        }

        if (TryComp<MeleeThrowOnHitComponent>(ent, out var throwOnHit))
        {
            if (ent.Comp.CachedThrowSpeed != null)
                throwOnHit.Speed = ent.Comp.CachedThrowSpeed.Value;

            if (ent.Comp.CachedThrowDistance != null)
                throwOnHit.Distance = ent.Comp.CachedThrowDistance.Value;

            Dirty(ent, throwOnHit);
        }

        _tag.RemoveTags(ent, ent.Comp.TagsToAdd);
    }

    /// <summary>
    /// Метод, обрабатывающий полное исцеление лица для перехода в стандартное состояние.
    /// </summary>
    private void OnFaceDamageChanged(Entity<Scp096FaceComponent> ent, ref DamageChangedEvent args)
    {
        if (!_mobThreshold.TryGetThresholdForState(ent, MobState.Alive, out var aliveThreshold))
            return;

        if (args.Damageable.TotalDamage == aliveThreshold)
            HealFace(ent);
    }

    private void OnHealingRelay(Entity<Scp096Component> ent, ref HealingRelayEvent args)
    {
        if (!TryGetFace(ent.AsNullable(), out var face))
            return;

        args.Entity = face;
    }

    /// <summary>
    /// Метод, полностью исцеляющий лицо скромника.
    /// Принимает самого скромника и автоматически получает его лицо.
    /// </summary>
    /// <param name="scp096"><see cref="EntityUid"/> скромника</param>
    /// <returns>Получилось вылечить лицо или нет</returns>
    private bool TryHealFace(EntityUid scp096)
    {
        if (!TryGetFace(scp096, out var face))
            return false;

        HealFace(face.Value);
        return true;
    }

    /// <summary>
    /// Метод, исцеляющий лицо скромника и переводящий его в живое состояние.
    /// Принимает лицо скромника в качестве аргумента.
    /// </summary>
    /// <param name="face">Лицо скромника</param>
    private void HealFace(Entity<Scp096FaceComponent> face)
    {
        // Лечим лицо и воскрешаем его.
        _mobState.ChangeMobState(face, MobState.Alive);

        if (TryComp<DamageableComponent>(face, out var damageable) && damageable.TotalDamage != FixedPoint2.Zero)
            _damageable.SetAllDamage(face, damageable, FixedPoint2.Zero);
    }
}
