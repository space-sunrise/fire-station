using Content.Shared.Random.Rules;

namespace Content.Shared._Scp.Other.Rules;

public sealed partial class AlwaysFalseRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        return Inverted;
    }
}
