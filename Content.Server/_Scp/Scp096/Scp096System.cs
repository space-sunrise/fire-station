using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Robust.Server.Audio;

namespace Content.Server._Scp.Scp096;

public sealed partial class Scp096System : SharedScp096System
{
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, MapInitEvent>(OnMapInit);

        InitializeTarget();
    }

    private void OnMapInit(Entity<Scp096Component> ent, ref MapInitEvent args)
    {
        SpawnFace(ent);

        UpdateAudio(ent.AsNullable(), ent.Comp.CrySound);
        _meta.AddFlag(ent, MetaDataFlags.PvsPriority);
    }

    protected override void OnShutdown(Entity<Scp096Component> ent, ref ComponentShutdown args)
    {
        base.OnShutdown(ent, ref args);

        UpdateAudio(ent.AsNullable(), setDefault: false);
        _meta.RemoveFlag(ent, MetaDataFlags.PvsPriority);
    }
}
