using Content.Shared.Damage;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Rounding;
using Robust.Shared.Utility;

namespace Content.Shared._Scp.Damage.ExaminableDamage;

/// <summary>
/// <para>Система для создания описания о повреждениях.</para>
/// Поддерживает живых сущностей и структуры.
/// Метод выведения сообщения определяется настройкой в компоненте <see cref="ScpExaminableDamageMode"/>
/// </summary>
/// <remarks>
/// <para>Для живых сущностей требуются компоненты <see cref="MobStateComponent"/> и <see cref="MobThresholdsComponent"/></para>
/// Для структуры требуется <see cref="DestructibleComponent"/>
/// </remarks>
public abstract class SharedScpExaminableDamageSystem : EntitySystem
{
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    private const int Priority = -99;
    public const double FullPercent = 1d;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpExaminableDamageComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<ScpExaminableDamageComponent> ent, ref ExaminedEvent args)
    {
        var target = GetProperEntity(ent);
        switch (ent.Comp.Mode)
        {
            case ScpExaminableDamageMode.Structure:
                StructureExamine(ent, target, ref args);
                break;

            case ScpExaminableDamageMode.MobToCritical:
                MobExamine(ent, target, MobState.Critical, ref args);
                break;

            case ScpExaminableDamageMode.MobToDeath:
                MobExamine(ent, target, MobState.Dead, ref args);
                break;

            default:
                throw new NotImplementedException($"Found not implemented type {ent.Comp.Mode}");
        }
    }

    private void MobExamine(Entity<ScpExaminableDamageComponent> ent, EntityUid target, MobState toState, ref ExaminedEvent args)
    {
        if (!_mobThreshold.TryGetThresholdForState(target, toState, out var max))
            return;

        CreateMessage(ent, target, max.Value, ref args);
    }

    protected virtual void StructureExamine(Entity<ScpExaminableDamageComponent> ent, EntityUid target, ref ExaminedEvent args) {}

    #region Helpers

    protected void CreateMessage(Entity<ScpExaminableDamageComponent> ent,
        EntityUid target,
        FixedPoint2 maxDamage,
        ref ExaminedEvent args)
    {
        var percent = GetDamagePercent(target, maxDamage);
        var level = ContentHelpers.RoundToNearestLevels(percent, FullPercent, ent.Comp.GeneralMessages.Count - 1);

        if (!ent.Comp.GeneralMessages.TryGetValue(level, out var message))
        {
            Log.Error($"Failed to get message with index {level}");
            return;
        }

        var formatted = $"\n[color={ent.Comp.Color.ToHex()}]{Loc.GetString(message)}[/color]";
        args.PushMarkup(formatted, Priority);
    }

    /// <summary>
    /// Возвращает значение между 0 и 1, показывающее степень повреждение сущности,
    /// где 0 это отсутствие урона, а 1 максимальная степень урона.
    /// </summary>
    /// <returns>Насколько сущность получила урона в оценке от 0 до 1</returns>
    public float GetDamagePercent(Entity<DamageableComponent?> ent, FixedPoint2 damageThreshold)
    {
        if (damageThreshold <= 0)
            return 0f;

        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        return (ent.Comp.TotalDamage / damageThreshold).Float();
    }

    /// <summary>
    /// Отправляет ивент, который может быть перехвачен системами, чтобы передать другую сущность для проверки урона.
    /// Может быть полезно, если требуется для одной сущности выводить описание урона другой сущности.
    /// </summary>
    /// <param name="defaultEntity">Сущность по умолчанию. Именно ее осмотрел игрок</param>
    /// <returns>Самую предпочтительную сущность для проверки на уровень урона.</returns>
    protected EntityUid GetProperEntity(EntityUid defaultEntity)
    {
        var ev = new ExaminableDamageRelayEvent();
        RaiseLocalEvent(defaultEntity, ref ev);

        return ev.Entity ?? defaultEntity;
    }

    #endregion
}

[ByRefEvent]
public record struct ExaminableDamageRelayEvent(EntityUid? Entity = null);
