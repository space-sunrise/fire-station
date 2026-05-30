
namespace Content.Shared._Scp.Other.ShowMessageOnSpawn;

public sealed partial class SharedShowMessageOnSpawnSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ShowMessageOnSpawnWindowOpenedEvent>(OnWindowOpened);
    }

    private void OnWindowOpened(ShowMessageOnSpawnWindowOpenedEvent ev, EntitySessionEventArgs args)
    {
        var uid = GetEntity(ev.TargetEntity);

        if (!TryComp<ShowMessageOnSpawnComponent>(uid, out var msgComp))
            return;

        if (msgComp.MessageShowed)
            return;

        msgComp.MessageShowed = true;
        Dirty(uid, msgComp);
    }
}
