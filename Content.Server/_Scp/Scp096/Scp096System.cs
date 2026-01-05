using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, MapInitEvent>(OnMapInit);

        InitializeTarget();
    }

    private void OnMapInit(Entity<Scp096Component> ent, ref MapInitEvent args)
    {
        _meta.AddFlag(ent, MetaDataFlags.PvsPriority);
    }

    protected override void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        _meta.RemoveFlag(ent, MetaDataFlags.PvsPriority);

        if (!_player.TryGetSessionByEntity(ent, out var session))
            return;

        // На случай, если компонент удален, когда имеются таргеты.
        var query = EntityQueryEnumerator<Scp096TargetComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            _pvsOverride.RemoveSessionOverride(uid, session);
        }
    }
}
