using Content.Shared._Scp.Fear.Components.Traits;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    private EntityQuery<FearStuporComponent> _fearStuporQuery;
    private EntityQuery<FearFaintingComponent> _fearFaintingQuery;

    private void InitializeTraits()
    {
        _fearStuporQuery = GetEntityQuery<FearStuporComponent>();
        _fearFaintingQuery = GetEntityQuery<FearFaintingComponent>();
    }
}
