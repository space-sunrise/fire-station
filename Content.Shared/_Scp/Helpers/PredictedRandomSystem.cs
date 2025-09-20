using Content.Shared.GameTicking;
using Content.Shared.Random.Helpers;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Helpers;

public sealed class PredictedRandomSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<NetEntity, System.Random> _entityRandoms = new();

    private System.Random? _tickRandom;
    private GameTick _lastTick;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _entityRandoms.Clear();
        _tickRandom = null;
        _lastTick = GameTick.Zero;
    }

    #region Entity-Based Random

    public int NextForEntity(EntityUid entity, int minValue, int maxValue)
    {
        var random = GetOrCreateEntityRandom(entity);
        return random.Next(minValue, maxValue);
    }

    public float NextFloatForEntity(EntityUid entity, float minValue = 0f, float maxValue = 1f)
    {
        var random = GetOrCreateEntityRandom(entity);
        return (float)(random.NextDouble() * (maxValue - minValue) + minValue);
    }

    public bool ProbForEntity(EntityUid entity, float chance)
    {
        var random = GetOrCreateEntityRandom(entity);
        return random.NextDouble() < chance;
    }

    private System.Random GetOrCreateEntityRandom(EntityUid entity)
    {
        var ent = GetNetEntity(entity);
        if (!_entityRandoms.TryGetValue(ent, out var random))
        {
            var seed = SharedRandomExtensions.HashCodeCombine(new (ent.Id));
            random = new System.Random(seed);
            _entityRandoms[ent] = random;
        }

        return random;
    }

    #endregion

    #region Tick-Based Random

    public int NextByTick(int minValue, int maxValue)
    {
        UpdateTickRandom();
        return _tickRandom!.Next(minValue, maxValue);
    }

    public float NextFloatByTick(float minValue = 0f, float maxValue = 1f)
    {
        UpdateTickRandom();
        return (float)(_tickRandom!.NextDouble() * (maxValue - minValue) + minValue);
    }

    public bool ProbByTick(float chance)
    {
        UpdateTickRandom();
        return _tickRandom!.NextDouble() < chance;
    }

    private void UpdateTickRandom()
    {
        var currentTick = _timing.CurTick;
        if (_lastTick != currentTick)
        {
            _lastTick = currentTick;
            _tickRandom = new System.Random((int)(currentTick.Value & 0x7FFFFFFF));
        }
    }

    #endregion

    #region Sequence-Based Random

    public PredictedRandomSequence CreateSequence(int baseSeed)
    {
        return new PredictedRandomSequence(baseSeed, (int)(_timing.CurTick.Value & 0x7FFFFFFF));
    }

    #endregion

    #region Legacy Support

    public int Next(int minValue, int maxValue)
    {
        return NextByTick(minValue, maxValue);
    }

    public float NextFloat(float minValue, float maxValue)
    {
        return NextFloatByTick(minValue, maxValue);
    }

    public bool Prob(float chance)
    {
        return ProbByTick(chance);
    }

    public T Pick<T>(IReadOnlyList<T> list)
    {
        UpdateTickRandom();
        var index = _tickRandom!.Next(list.Count);
        return list[index];
    }

    #endregion
}

public sealed class PredictedRandomSequence
{
    private readonly System.Random _random;
    private int _callCount;

    public PredictedRandomSequence(int baseSeed, int tickSeed)
    {
        var combinedSeed = HashCode.Combine(baseSeed, tickSeed);
        _random = new System.Random(combinedSeed);
        _callCount = 0;
    }

    public int Next(int minValue, int maxValue)
    {
        _callCount++;
        return _random.Next(minValue, maxValue);
    }

    public float NextFloat(float minValue = 0f, float maxValue = 1f)
    {
        _callCount++;
        return (float)(_random.NextDouble() * (maxValue - minValue) + minValue);
    }

    public bool Prob(float chance)
    {
        _callCount++;
        return _random.NextDouble() < chance;
    }

    public void Skip(int count)
    {
        for (int i = 0; i < count; i++)
        {
            _random.Next();
            _callCount++;
        }
    }

    public int CallCount => _callCount;
}

