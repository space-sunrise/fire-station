using Content.Shared._Scp.Animations.Scale;
using Robust.Shared.Player;

namespace Content.Server._Scp.Animations.Scale;

public sealed class ScaleAnimationSystem : SharedScaleAnimationSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScaleAnimationComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ScaleAnimationComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.AnimationEndTime = Timing.CurTime + ent.Comp.Duration;

        var ev = new ScaleAnimationStartEvent(GetNetEntity(ent));
        RaiseNetworkEvent(ev, Filter.Pvs(ent));
    }
}
