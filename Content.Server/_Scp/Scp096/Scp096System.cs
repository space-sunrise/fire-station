using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Robust.Server.GameStates;

namespace Content.Server._Scp.Scp096;

public sealed class Scp096System : SharedScp096System
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;

    protected override void AddTarget(Entity<Scp096Component> scp, EntityUid target)
    {
        base.AddTarget(scp, target);

        _pvsOverride.AddGlobalOverride(target);
    }

    protected override void RemoveTarget(Entity<ActiveScp096RageComponent?> scp, EntityUid target, bool removeComponent = true)
    {
        base.RemoveTarget(scp, target, removeComponent);

        _pvsOverride.RemoveGlobalOverride(target);
    }
}
