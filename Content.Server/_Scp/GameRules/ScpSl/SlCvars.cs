using Robust.Shared.Configuration;

namespace Content.Server._Scp.GameRules.ScpSl;

[CVarDefs]
public sealed class SlCvars
{
    public static readonly CVarDef<int> MaxMogSpawnCount =
        CVarDef.Create("sl.max_mog_spawn_count", 20, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> MaxChaosSpawnCount =
        CVarDef.Create("sl.max_chaos_spawn_count", 20, CVar.SERVERONLY | CVar.ARCHIVE);

    public static readonly CVarDef<float> ChaosSpawnChance =
        CVarDef.Create("sl.chaos_spawn_chance", 0.2f, CVar.SERVERONLY | CVar.ARCHIVE);
}
