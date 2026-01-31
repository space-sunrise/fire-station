using Content.Shared.Damage;

namespace Content.Shared._Scp.Other.Events;

[ByRefEvent]
public record struct DamageChangedOriginEvent(EntityUid Target, DamageSpecifier Damage);
