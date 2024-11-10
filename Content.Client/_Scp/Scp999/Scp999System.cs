using Content.Shared._Scp.Scp999;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.Scp999;

public sealed class Scp999System : SharedScp999System
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<Scp999WallifyEvent>(OnWallify);
        SubscribeNetworkEvent<Scp999RestEvent>(OnRest);
    }

    private void OnWallify(Scp999WallifyEvent args)
    {
        var uid = GetEntity(args.NetEntity);

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        spriteComponent.LayerSetState(Scp999States.Default, args.TargetState);
    }

    private void OnRest(Scp999RestEvent args)
    {
        var uid = GetEntity(args.NetEntity);

        if (!TryComp<SpriteComponent>(uid, out var spriteComponent))
            return;

        spriteComponent.LayerSetState(Scp999States.Default, args.TargetState);
    }
}
