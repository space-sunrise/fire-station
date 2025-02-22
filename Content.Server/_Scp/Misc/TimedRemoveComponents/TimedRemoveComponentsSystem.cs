using Robust.Shared.Timing;

namespace Content.Server._Scp.Misc.TimedRemoveComponents;

public sealed class TimedRemoveComponentsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TimedRemoveComponentsComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<TimedRemoveComponentsComponent> ent, ref ComponentInit args)
    {
        Timer.Spawn(ent.Comp.RemoveAfter, () => RemoveComponents(ent));
    }

    private void RemoveComponents(Entity<TimedRemoveComponentsComponent> ent)
    {
        EntityManager.RemoveComponents(ent, ent.Comp.Components);

        // блять, я себя захуярил
        RemComp<TimedRemoveComponentsComponent>(ent);
    }
}
