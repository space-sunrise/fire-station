using Content.Server.Explosion.EntitySystems;

namespace Content.Server._Scp.Misc.AddComponentsOnTrigger;

public sealed class AddComponentsOnTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AddComponentsOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<AddComponentsOnTriggerComponent> entity, ref TriggerEvent args)
    {
        EntityManager.AddComponents(entity, entity.Comp.Components);
    }
}
