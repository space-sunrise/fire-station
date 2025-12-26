using Content.Shared._Scp.Other.ClassDAppearance;
using Content.Shared.Random.Rules;
using JetBrains.Annotations;

namespace Content.Shared._Scp.Other.Rules;

[UsedImplicitly]
public sealed partial class IsClassD : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.HasComponent<ClassDAppearanceComponent>(uid))
            return Inverted;

        return !Inverted;
    }
}
