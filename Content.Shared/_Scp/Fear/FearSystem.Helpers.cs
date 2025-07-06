using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Shaders;

namespace Content.Shared._Scp.Fear;

public sealed partial class FearSystem
{
    private const int MinPossibleValue = (int) FearState.None;
    private const int MaxPossibleValue = (int) FearState.Terror;

    /// <summary>
    /// Рассчитывает актуальную силу шейдеров, учитывая силу шейдера от уровня страха и некую другую силу.
    /// Например, некой другой силой может быть сила от эффекта/приближения или еще чего-то.
    /// Задача метода не дать этой силе быть меньше, чем силе основанной на текущем уровне страха.
    /// </summary>
    private static float GetActualStrength<T>(FearComponent fear, float strength)
        where T : IShaderStrength, IComponent
    {
        var fearBasedStrength = fear.CurrentFearBasedShaderStrength.GetValueOrDefault(typeof(T).Name);
        Logger.Warning($"{fearBasedStrength}, {typeof(T).Name}");
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
        if (currentRange <= 0f)
            return parameters.Max;

        var proximityFactor = 1f - Math.Clamp(currentRange / maxRange, 0f, 1f);

        return MathHelper.Lerp(parameters.Min, parameters.Max, proximityFactor);
    }

    /// <summary>
    /// Уменьшает уровень страха на 1, не позволяя опуститься ниже FearState.None.
    /// </summary>
    public static FearState GetDecreasedLevel(FearState state)
    {
        var newValue = (int) state - 1;

        return (FearState) Math.Max(MinPossibleValue, newValue);
    }

    /// <summary>
    /// Увеличивает уровень страха на 1, не позволяя подняться выше FearState.Terror.
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
