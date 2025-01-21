using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Random.Rules;

namespace Content.Shared._Scp.Other.Rules;

public sealed partial class InScp106Dimension : RulesRule
{
    public override bool Check(EntityManager entManager, EntityUid uid)
    {
        if (!entManager.TryGetComponent(uid, out TransformComponent? xform) || !entManager.HasComponent<Scp106BackRoomMapComponent>(xform.MapUid))
            return Inverted;

        return !Inverted;
    }
}
