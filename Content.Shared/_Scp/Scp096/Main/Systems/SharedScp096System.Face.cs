using System.Diagnostics.CodeAnalysis;
using Content.Shared._Scp.Damage.ExaminableDamage;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Healing;
using Content.Shared.Mobs;
using Content.Shared.Rounding;
using JetBrains.Annotations;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedScpExaminableDamageSystem _examinableDamage = default!;

    private void InitializeFace()
    {
        SubscribeLocalEvent<Scp096FaceComponent, DamageChangedEvent>(OnFaceDamageChanged);
        SubscribeLocalEvent<Scp096FaceComponent, MobStateChangedEvent>(OnFaceMobStateChanged);

        SubscribeLocalEvent<Scp096Component, HealingRelayEvent>(OnHealingRelay);
        SubscribeLocalEvent<Scp096Component, ExaminableDamageRelayEvent>(OnExaminableRelay);
    }

    #region Event handlers

    /// <summary>
    /// Метод, обрабатывающий полное исцеление лица для перехода в стандартное состояние.
    /// </summary>
    private void OnFaceDamageChanged(Entity<Scp096FaceComponent> ent, ref DamageChangedEvent args)
    {
        if (TryGetScp096FromFace(ent, out var owner))
            ActualizeAlert(owner.Value);

        if (!_mobThreshold.TryGetThresholdForState(ent, MobState.Alive, out var aliveThreshold))
            return;

        if (args.Damageable.TotalDamage == aliveThreshold)
            HealFace(ent);
    }

    private void OnFaceMobStateChanged(Entity<Scp096FaceComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.FaceOwner.HasValue)
        {
            Log.Error("Found SCP-096 face without reference to original SCP-096");
            return;
        }

        switch (args.NewMobState)
        {
            case MobState.Dead:
            case MobState.Critical:
                EnsureComp<ActiveScp096WithoutFaceComponent>(ent.Comp.FaceOwner.Value);
                break;
            case MobState.Invalid:
            case MobState.Alive:
                // Используем RemCompDeferred, чтобы избежать конфликта с уже запланированным удалением
                // (например, при воскрешении через OnRejuvenate)
                RemCompDeferred<ActiveScp096WithoutFaceComponent>(ent.Comp.FaceOwner.Value);
                break;
        }
    }

    private void OnHealingRelay(Entity<Scp096Component> ent, ref HealingRelayEvent args)
    {
        if (!TryGetFace(ent.AsNullable(), out var face))
            return;

        args.Entity = face;
    }

    private void OnExaminableRelay(Entity<Scp096Component> ent, ref ExaminableDamageRelayEvent args)
    {
        if (!TryGetFace(ent.AsNullable(), out var face))
            return;

        args.Entity = face;
    }

    #endregion

    #region API

    [PublicAPI]
    public bool TryGetFace(Entity<Scp096Component?> ent, [NotNullWhen(true)] out Entity<Scp096FaceComponent>? face)
    {
        face = null;

        if (IsClientSide(ent))
            return false;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!Exists(ent.Comp.FaceEntity))
        {
            Log.Error($"Found SCP-096 without valid face entity. Scp096 is {ToPrettyString(ent)}, while reference is {ToPrettyString(ent.Comp.FaceEntity)}");
            return false;
        }

        var faceEntity = ent.Comp.FaceEntity.Value;

        if (!FaceQuery.TryComp(faceEntity, out var faceComp))
        {
            Log.Error($"Found SCP-096 face without {nameof(Scp096FaceComponent)}! Prototype: {Prototype(faceEntity)}, Entity: {ToPrettyString(faceEntity)}");
            return false;
        }

        face = (faceEntity, faceComp);
        return true;
    }

    [PublicAPI]
    public bool TryGetScp096FromFace(Entity<Scp096FaceComponent> ent, [NotNullWhen(true)] out Entity<Scp096Component>? scp096)
    {
        scp096 = null;

        if (!Exists(ent.Comp.FaceOwner))
        {
            Log.Error($"Found SCP-096 face entity with unexisting owner. Face - {ToPrettyString(ent)}, Owner - {ToPrettyString(ent.Comp.FaceOwner)}");
            return false;
        }

        if (!Scp096Query.TryComp(ent.Comp.FaceOwner, out var scp096Comp))
        {
            Log.Error($"Found SCP-096 face owner without {nameof(Scp096Component)}. Face - {ToPrettyString(ent)}, Owner - {ToPrettyString(ent.Comp.FaceOwner)}");
            return false;
        }

        scp096 = (ent.Comp.FaceOwner.Value, scp096Comp);
        return true;
    }

    /// <summary>
    /// <para> Пытается просчитать текущее состояние алерта для поврежденного лица. </para>
    /// Если лицо мертво - возвращает максимальное состояние.
    /// </summary>
    /// <param name="uid"><see cref="EntityUid"/> скромника</param>
    /// <param name="severity">Состояние алерта для поврежденного лица</param>
    /// <returns>
    /// <para> True: Удалось просчитать значение для состояния лица. </para>
    /// False: Не удалось
    /// </returns>
    [PublicAPI]
    public bool TryGetAlertDamageSeverity(EntityUid uid, out short severity)
    {
        var min = _alerts.GetMinSeverity(FaceDamageAlert);
        var max = _alerts.GetMaxSeverity(FaceDamageAlert);

        return TryGetFaceDamageLevel(uid, min, max, out severity);
    }

    /// <summary>
    /// <para> Пытается просчитать текущий уровень повреждения лица скромника</para>
    /// Если лицо мертво - возвращает максимальное состояние.
    /// </summary>
    /// <param name="uid"><see cref="EntityUid"/> скромника</param>
    /// <param name="min">Минимальный уровень неповрежденности лица. Обычно должен быть 0</param>
    /// <param name="max">Максимальный уровень неповрежденности лица. </param>
    /// <param name="level">Просчитанный уровень неповрежденности лица</param>
    /// <returns>
    /// <para> True: Удалось просчитать значение для состояния лица. </para>
    /// False: Не удалось
    /// </returns>
    [PublicAPI]
    public bool TryGetFaceDamageLevel(EntityUid uid, int min, int max, out short level)
    {
        level = 0;

        if (!TryGetFace(uid, out var face))
            return false;

        // Если лицо мертво - используем максимальное состояние сразу.
        if (_mobState.IsDead(face.Value))
        {
            level = (short) max;
            return true;
        }

        var deathThreshold = _mobThreshold.GetThresholdForState(face.Value, MobState.Dead);
        if (deathThreshold <= FixedPoint2.Zero)
            return false;

        var levels = max - min;
        level = (short) GetDamageLevel(face.Value, deathThreshold, levels);

        return true;
    }

    #endregion

    #region Helpers

    private bool TryToggleTears(Entity<Scp096Component?> ent, bool value)
    {
        if (!TryGetFace(ent, out var face))
            return false;

        ToggleTears(face.Value, value);
        return true;
    }

    private bool TryToggleTearsReagent(Entity<Scp096Component?> ent, bool useDefaultReagent)
    {
        if (!TryGetFace(ent, out var face))
            return false;

        ToggleTearsReagent(face.Value, useDefaultReagent);
        return true;
    }

    private bool TryModifyTearsSpawnSpeed(Entity<Scp096Component?> ent, bool cryFaster)
    {
        if (!TryGetFace(ent, out var face))
            return false;

        ModifyTearsSpawnSpeed(face.Value, cryFaster);
        return true;
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
        if (TryComp<DamageableComponent>(face, out var damageable) && damageable.TotalDamage != FixedPoint2.Zero)
            _damageable.SetAllDamage(face, damageable, FixedPoint2.Zero);

        // Лечим лицо и воскрешаем его.
        _mobState.ChangeMobState(face, MobState.Alive);
    }

    private int GetDamageLevel(EntityUid target, FixedPoint2 maxDamage, int levels)
    {
        var percent = _examinableDamage.GetDamagePercent(target, maxDamage);
        var level = ContentHelpers.RoundToNearestLevels(percent, SharedScpExaminableDamageSystem.FullPercent, levels);

        return level;
    }

    #endregion

    #region Virtuals

    protected virtual void ToggleTears(Entity<Scp096FaceComponent> ent, bool value) { }

    protected virtual void ToggleTearsReagent(Entity<Scp096FaceComponent> ent, bool useDefaultReagent) { }

    protected virtual void ModifyTearsSpawnSpeed(Entity<Scp096FaceComponent> ent, bool cryFaster) { }

    #endregion
}
