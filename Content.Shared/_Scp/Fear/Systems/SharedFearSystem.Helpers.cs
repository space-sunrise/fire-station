using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Shaders;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    private const int MinPossibleValue = (int) FearState.None;
    private const int MaxPossibleValue = (int) FearState.Terror;

    /// <summary>
    /// Подсвечивает все сущности, которые вызывают страх.
    /// Сущности, чей уровень страха при видимости отличается от текущего уровня страха не будет подсвечены.
    /// </summary>
    private void HighLightAllVisibleFears(Entity<FearComponent> ent)
    {
        var visibleFearSources =
            _watching.GetAllEntitiesVisibleTo<FearSourceComponent>(ent.Owner, ent.Comp.SeenBlockerLevel);

        foreach (var source in visibleFearSources)
        {
            if (source.Comp.UponSeenState != ent.Comp.State)
                continue;

            _highlight.Highlight(source);
        }
    }

    /// <summary>
    /// Рассчитывает актуальную силу шейдеров, учитывая силу шейдера от уровня страха и некую другую силу.
    /// Например, некой другой силой может быть сила от эффекта/приближения или еще чего-то.
    /// Задача метода не дать этой силе быть меньше, чем силе основанной на текущем уровне страха.
    /// </summary>
    private static float GetActualStrength<T>(FearComponent fear, float strength)
        where T : IShaderStrength, IComponent
    {
        var fearBasedStrength = fear.CurrentFearBasedShaderStrength.GetValueOrDefault(typeof(T).Name);
        var actualStrength = Math.Clamp(strength, fearBasedStrength, float.MaxValue);

        return actualStrength;
    }

    /// <summary>
    /// Рассчитывает силу шейдера исходя из приближения сущности к источнику страха.
    /// Чем ближе сущность, чем больше будет сила. Рассчитывается из процентного соотношения.
    /// </summary>
    /// <param name="currentRange">Текущее расстояние до источника страха</param>
    /// <param name="maxRange">Порог входа в зону действия источника страха</param>
    /// <param name="parameters">Параметры силы шейдера. Передает, какие будут значения на границах.</param>
    /// <returns>Рассчитанную силу шейдера</returns>
    private static float CalculateShaderStrength(float currentRange, float maxRange, MinMaxExtended parameters)
    {
        return CalculateStrength(currentRange, maxRange, parameters.Min, parameters.Max);
    }

    /// <summary>
    /// Рассчитывает силу чего-либо исходя из приближения к источнику страха.
    /// Более универсальный метод, не требующий <see cref="MinMaxExtended"/>
    /// </summary>
    private static float CalculateStrength(float currentRange, float maxRange, float min, float max, bool inverse = false)
    {
        if (currentRange <= 0f)
            return max;

        if (currentRange >= maxRange)
            return min;

        // Фактор близости: 1.0 = вплотную, 0.0 = на максимальном расстоянии
        var proximityFactor = 1f - (currentRange / maxRange);

        if (inverse)
            return MathHelper.Lerp(max, min, proximityFactor);

        return MathHelper.Lerp(min, max, proximityFactor);
    }

    /// <summary>
    /// Возвращает уровень страха уменьшенный на 1, не позволяя опуститься ниже <see cref="FearState.None"/>
    /// </summary>
    public static FearState GetDecreasedLevel(FearState state)
    {
        var newValue = (int) state - 1;

        return (FearState) Math.Max(MinPossibleValue, newValue);
    }

    /// <summary>
    /// Возвращает уровень страха увеличенный на 1, не позволяя подняться выше <see cref="FearState.Terror"/>
    /// </summary>
    public static FearState GetIncreasedLevel(FearState state)
    {
        var newValue = (int) state + 1;

        return (FearState) Math.Min(MaxPossibleValue, newValue);
    }

    private void SetNextCalmDownTime(Entity<FearComponent> ent)
    {
        ent.Comp.NextTimeDecreaseFearLevel = _timing.CurTime + ent.Comp.TimeToDecreaseFearLevel;
        Dirty(ent);
    }
}
