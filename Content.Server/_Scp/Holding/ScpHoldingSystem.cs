using Content.Shared.Hands;
using Content.Shared._Scp.Holding.Components;
using Content.Shared._Scp.Holding.Systems;

namespace Content.Server._Scp.Holding;

public sealed partial class ScpHoldingSystem : SharedScpHoldingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpHolderComponent, ComponentShutdown>(OnHoldShutdown);
        SubscribeLocalEvent<ActiveScpHoldableComponent, HandCountChangedEvent>(OnHandCountChanged);
    }

    protected override void OnHeldStateShutdown(Entity<ActiveScpHoldableComponent> held)
    {
        foreach (var holderUid in held.Comp.Holders.ToArray())
        {
            RemComp<ActiveScpHolderComponent>(holderUid);
        }
    }

    private void OnHoldShutdown(Entity<ScpHolderComponent> ent, ref ComponentShutdown args)
    {
        if (!TryComp<ActiveScpHolderComponent>(ent, out var holder))
            return;

        if (holder.Target == null)
            return;

        ReleaseHolderContribution(ent, holder.Target.Value, clearIfEmpty: true);
    }

    private void OnHandCountChanged(Entity<ActiveScpHoldableComponent> ent, ref HandCountChangedEvent args)
    {
        SyncHeldState(ent);
    }
}
