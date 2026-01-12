using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components;
using Content.Shared.Random.Rules;
using JetBrains.Annotations;

namespace Content.Shared._Scp.Other.Rules;

[UsedImplicitly]
public sealed partial class FearStateRule : RulesRule
{
    [DataField]
    public FearState RequiredState;

    [DataField]
    public bool CanBeGreater;

    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent<FearComponent>(uid, out var fear))
            return Inverted;

        var should = CanBeGreater
            ? fear.State >= RequiredState
            : fear.State == RequiredState;

        if (!should)
            return Inverted;

        return !Inverted;
    }
}
