namespace Content.Shared._Scp.Scp330;

/// <summary>
/// Ивент, вызывающийся при применении наказания SCP-330.
/// Вызывается на цели.
/// </summary>
/// <param name="Bowl">Миска SCP-330</param>
[ByRefEvent]
public record struct Scp330TargetPunishmentEvent(EntityUid Bowl);

/// <summary>
/// Ивент, вызываемый при применении наказания SCP-330.
/// Вызывается на миске SCP-330
/// </summary>
/// <param name="Target">Цель наказания</param>
[ByRefEvent]
public record struct Scp330SelfPunishmentEvent(EntityUid Target);
