using Content.Shared.Trigger.Systems; // Путь, который ты нашел
using Robust.Shared.GameObjects;

namespace Content.Shared._Scp.Research.Interact;

public sealed class TriggerOnScpResearchSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!; 

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TriggerOnScpResearchComponent, ScpResearchInteractSuccessfulEvent>(OnResearchSuccess);
    }

    private void OnResearchSuccess(EntityUid uid, TriggerOnScpResearchComponent component, ref ScpResearchInteractSuccessfulEvent args)
    {
        // uid - это SCP, args.User - это тот, кто проводил исследование.
        _trigger.Trigger(uid, args.User);
    }
}

[RegisterComponent]
public sealed partial class TriggerOnScpResearchComponent : Component 
{
}