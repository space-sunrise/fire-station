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
        SetSeed(sides);

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
        SetSeed(minValue);

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

        return _random.Next(minValue, maxValue);
    }

    public int Next(int minValue, int maxValue, int value)
    {
        SetSeed(value);

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

        return _random.Next(minValue, maxValue);
    }

    public double NextDouble()
    {
        SetSeed();
        return _random.NextDouble();
    }

    public double NextDouble(EntityUid uid)
    {
        SetSeed(GetNetEntity(uid).Id);
        return _random.NextDouble();
    }

    public double NextDouble(double minValue, double maxValue)
    {
        SetSeed(minValue.GetHashCode());

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

        return _random.NextDouble() * (maxValue - minValue) + minValue;
    }

    public double NextDouble(EntityUid uid, float minValue, float maxValue)
    {
        SetSeed(GetNetEntity(uid).Id);

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

        return _random.NextDouble() * (maxValue - minValue) + minValue;
    }

    public float NextFloat()
    {
        SetSeed();
        return _random.NextFloat();
    }

    public float NextFloat(EntityUid uid)
    {
        SetSeed(GetNetEntity(uid).Id);
        return _random.NextFloat();
    }

    public float NextFloat(float minValue, float maxValue)
    {
        SetSeed(minValue.GetHashCode());

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

        return _random.NextFloat() * (maxValue - minValue) + minValue;
    }

    public float NextFloat(EntityUid uid, float minValue, float maxValue)
    {
        SetSeed(GetNetEntity(uid).Id);

        if (minValue > maxValue)
            (minValue, maxValue) = (maxValue, minValue);

        return _random.NextFloat() * (maxValue - minValue) + minValue;
    }

    #endregion

    #region Prob

    public bool Prob(float chance)
    {
        DebugTools.Assert(chance <= 1 && chance >= 0, $"Chance must be in the range 0-1. It was {chance}.");

        SetSeed(chance.GetHashCode());
        var c = Math.Clamp(chance, 0f, 1f);
        return _random.NextDouble() < c;
    }

    #endregion

    #region Pick

    public T Pick<T>(IReadOnlyList<T> list)
    {
        DebugTools.Assert(list.Count > 0, "Pick called with empty list");

        if (list.Count == 0)
            return default!;

        SetSeed(list[0]!.GetHashCode());

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
