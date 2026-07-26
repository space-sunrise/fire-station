using Content.Shared._Scp.Scp082;

namespace Content.Client._Scp.Scp082;

public sealed class Scp082System : SharedScp082System
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Scp082Component, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
    }

    private void OnAfterAutoHandleState(Entity<Scp082Component> entity, ref AfterAutoHandleStateEvent args)
    {
        UpdateDigestibility(entity);
    }
}
