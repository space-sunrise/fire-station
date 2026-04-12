#pragma warning disable IDE0130 // Namespace does not match folder structure

using Content.Shared.Access.Components;
using Content.Shared.Movement.Components;

namespace Content.Shared.Silicons.StationAi;

public abstract partial class SharedStationAiSystem
{
    private void InitializeAccess()
    {
        SubscribeLocalEvent<MovementRelayTargetComponent, GetAdditionalAccessEvent>(OnRelayGetAdditionalAccess);
    }

    private void OnRelayGetAdditionalAccess(Entity<MovementRelayTargetComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (!HasComp<StationAiHeldComponent>(ent.Comp.Source))
            return;

        args.Entities.Add(ent.Comp.Source);
    }
}
