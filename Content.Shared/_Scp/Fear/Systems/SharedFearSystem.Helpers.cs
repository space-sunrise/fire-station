using System.Threading;
using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Shaders;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    private static CancellationTokenSource _restartToken = new ();

    private const int MinPossibleValue = (int) FearState.None;
    private const int MaxPossibleValue = (int) FearState.Terror;

    private const int GenericFearBasedScaleModifier = 2;

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

            _highlight.Highlight(source, ent);
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

    /// <summary>
    /// Проверяет, растет ли уровень страха на основе переданных нового уровня и старого уровня страха.
    /// </summary>
    public static bool IsIncreasing(FearState newState, FearState oldState)
    {
        return newState > oldState;
    }

    /// <summary>
    /// Возвращает стандартный модификатор, зависящий от текущего уровня страха.
    /// По умолчанию используется квадратичный рост модификатора с уровнем страха.
    /// </summary>
    public static int GetGenericFearBasedModifier(FearState state, int scale = GenericFearBasedScaleModifier)
    {
        return (int) Math.Pow((int) state, scale);
    }

    /// <summary>
    /// Устанавливает следующее время попытки сущности успокоиться
    /// </summary>
    protected void SetNextCalmDownTime(Entity<FearComponent> ent)
    {
        ent.Comp.NextTimeDecreaseFearLevel = _timing.CurTime + ent.Comp.TimeToDecreaseFearLevel;
        Dirty(ent);
    }

    protected void RemoveComponentAfter<T>(EntityUid ent, float removeAfter) where T : IComponent
    {
        Timer.Spawn(TimeSpan.FromSeconds(removeAfter), () => RemComp<T>(ent), _restartToken.Token);
    }

    protected void RemoveComponentAfter<T>(EntityUid ent, TimeSpan removeAfter) where T : IComponent
    {
        Timer.Spawn(removeAfter, () => RemComp<T>(ent), _restartToken.Token);
    }

    /// <summary>
    /// Возвращает переведенную строку, содержащую ИЦ информацию об уровне страха человека.
    /// Если уровень страха отсутствует, то возвращает null
    /// </summary>
    private string? GetExamineText(EntityUid target, FearState type) => type switch
    {
        FearState.None => null,
        FearState.Anxiety => Loc.GetString("examine-fear-state-anxiety", ("target", target)),
        FearState.Fear => Loc.GetString("examine-fear-state-fear", ("target", target)),
        FearState.Terror => Loc.GetString("examine-fear-state-terror", ("target", target)),

        _ => null,
    };

    /// <summary>
    /// Возвращает переведенную строку, содержащую ИЦ информацию об уровне страха человека.
    /// Если уровень страха отсутствует, то возвращает null.
    /// Вариант для мертвых тел, описывающий застывшее на лице состояние в зависимости от уровня страха.
    /// </summary>
    private string? GetDeadExamineText(EntityUid target, FearState type) => type switch
    {
        FearState.None => Loc.GetString("examine-fear-state-none-dead", ("target", target)),
        FearState.Anxiety => Loc.GetString("examine-fear-state-anxiety-dead", ("target", target)),
        FearState.Fear => Loc.GetString("examine-fear-state-fear-dead", ("target", target)),
        FearState.Terror => Loc.GetString("examine-fear-state-terror-dead", ("target", target)),

        _ => null,
    };

    /// <summary>
    /// Преобразует процент из человеческого формата в probный.
    /// </summary>
    protected static float PercentToNormalized(float percent)
    {
        return Math.Clamp(percent / 100f, 0f, 1f);
    }

    protected virtual void Clear()
    {
        _restartToken.Cancel();
        _restartToken = new();
    }
}
