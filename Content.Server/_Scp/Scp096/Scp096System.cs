using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Robust.Shared.Player;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    public override void Initialize()
    {
        base.Initialize();

        InitializeTarget();

        SubscribeLocalEvent<Scp096Component, PlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<Scp096Component, PlayerDetachedEvent>(OnDetached);
    }

    protected override void OnInit(Entity<Scp096Component> ent, ref ComponentInit args)
    {
        base.OnInit(ent, ref args);

        _meta.AddFlag(ent, MetaDataFlags.PvsPriority);
        TryAddOverrides(ent);
    }

    protected override void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        _meta.RemoveFlag(ent, MetaDataFlags.PvsPriority);
        TryRemoveOverrides(ent);
    }

    private void OnAttached(Entity<Scp096Component> ent, ref PlayerAttachedEvent args)
    {
        TryAddOverrides(ent);
    }

    private void OnDetached(Entity<Scp096Component> ent, ref PlayerDetachedEvent args)
    {
        TryRemoveOverrides(ent);
    }

    private bool TryAddOverrides(EntityUid scp)
    {
        if (!_player.TryGetSessionByEntity(scp, out var session))
            return false;

        var query = EntityQueryEnumerator<Scp096TargetComponent>();
        while (query.MoveNext(out var targetUid, out _))
        {
            _pvsOverride.AddSessionOverride(targetUid, session);
        }

        return true;
    }

    private bool TryRemoveOverrides(EntityUid scp)
    {
        if (!_player.TryGetSessionByEntity(scp, out var session))
            return false;

        var query = EntityQueryEnumerator<Scp096TargetComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.RemoveSessionOverride(uid, session);
        }

        return true;
    }
}
