using Content.Shared.Random.Rules;
using JetBrains.Annotations;

namespace Content.Shared._Scp.Other.Rules;

[UsedImplicitly]
public sealed partial class AlwaysFalseRule : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        return Inverted;
    }
}
