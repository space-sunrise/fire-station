using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Random.Rules;
using JetBrains.Annotations;

namespace Content.Shared._Scp.Other.Rules;

[UsedImplicitly]
public sealed partial class MobStateRule : RulesRule
{
    [DataField]
    public HashSet<MobState> AllowedStates = [];

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent<MobStateComponent>(uid, out var mobState))
            return Inverted;

        if (!AllowedStates.Contains(mobState.CurrentState))
            return Inverted;

        return !Inverted;
    }
}
