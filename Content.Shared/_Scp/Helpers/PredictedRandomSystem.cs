using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._Scp.Helpers;

public sealed class PredictedRandomSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private System.Random _random = new();

    #region Roll

    private int Roll(int sides)
    {
        return _random.Next(1, sides + 1);
    }

    public int RollWith(int sides, int numberOfDice)
    {
        SetSeed();

        var total = 0;
        for (var i = 0; i < numberOfDice; i++)
        {
            total += Roll(sides);
        }

        return total;
    }

    #endregion

    #region Next

    public int Next(int minValue, int maxValue)
    {
        SetSeed();
        return _random.Next(minValue, maxValue);
    }

    public int Next(int minValue, int maxValue, int value)
    {
        SetSeed(value);
        return _random.Next(minValue, maxValue);
    }

    public double NextDouble()
    {
        SetSeed();
        return _random.NextDouble();
    }

    public float NextFloat(float minValue, float maxValue)
    {
        SetSeed();
        return _random.NextFloat() * (maxValue - minValue) + minValue;
    }

    #endregion

    #region Prob

    public bool Prob(float chance)
    {
        DebugTools.Assert(chance <= 1 && chance >= 0, $"Chance must be in the range 0-1. It was {chance}.");

        SetSeed();
        return _random.NextDouble() < chance;
    }

    #endregion

    #region Pick

    public T Pick<T>(IReadOnlyList<T> list)
    {
        SetSeed();

        var index = _random.Next(list.Count);
        return list[index];
    }

    #endregion

    #region Private

    private void SetSeed()
    {
        var currentTick = _timing.CurTime.Milliseconds.GetHashCode();
        _random = new System.Random(currentTick);
    }

    private void SetSeed(int value)
    {
        var currentTick = _timing.CurTime.Milliseconds.GetHashCode();
        var valueHash = value.GetHashCode();

        var hash = HashCode.Combine(currentTick, valueHash);

        _random = new System.Random(hash);
    }

    #endregion
}
