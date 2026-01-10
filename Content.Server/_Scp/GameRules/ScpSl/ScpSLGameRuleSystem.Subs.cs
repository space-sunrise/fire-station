namespace Content.Server._Scp.GameRules.ScpSl;

public sealed partial class ScpSlGameRuleSystem
{
    private int _maxChaosSpawnCount;
    private int _maxMogSpawnCount;
    private float _chaosSpawnChance;
    private void InitializeSubs()
    {
        _cfg.OnValueChanged(SlCvars.MaxChaosSpawnCount, newValue => _maxChaosSpawnCount = newValue);
        _cfg.OnValueChanged(SlCvars.MaxMogSpawnCount, newValue => _maxMogSpawnCount = newValue);
        _cfg.OnValueChanged(SlCvars.ChaosSpawnChance, newValue => _chaosSpawnChance = newValue);
    }
}
