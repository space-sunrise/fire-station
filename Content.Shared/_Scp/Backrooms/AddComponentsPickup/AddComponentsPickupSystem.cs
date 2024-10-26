using Content.Shared.Interaction.Events;
using Content.Shared.Item;

namespace Content.Shared._Scp.Backrooms;

public sealed class AddComponentsPickupComponentSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsPickupComponent, GettingPickedUpAttemptEvent>(OnPickUp);
        SubscribeLocalEvent<AddComponentsPickupComponent, DroppedEvent>(OnDrop);
    }

    private void OnPickUp(Entity<AddComponentsPickupComponent> entity, ref GettingPickedUpAttemptEvent args)
    {
        EntityManager.AddComponents(args.User, entity.Comp.Components);
    }

    private void OnDrop(Entity<AddComponentsPickupComponent> entity, ref DroppedEvent args)
    {
        EntityManager.RemoveComponents(args.User, entity.Comp.Components);
    }
}
