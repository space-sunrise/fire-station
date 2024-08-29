using Content.Shared.Actions;

namespace Content.Shared._Scp.Actions;

public sealed class GiveActionsSystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GiveActionsComponent, MapInitEvent>(OnSpawn);
    }

    private void OnSpawn(EntityUid uid, GiveActionsComponent component, MapInitEvent args)
    {
        foreach (var action in component.Actions)
        {
            _actions.AddAction(uid, action);
        }

        // No longer needed after actions are given
        RemComp<GiveActionsComponent>(uid);
    }
}
