using Content.Server.Flash;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Server._Scp.Eye.Blinking;

public sealed class EyeClosingSystem : SharedEyeClosingSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeClosingComponent, FlashAttemptEvent>(OnFlash);
    }

    private void OnFlash(Entity<EyeClosingComponent> ent, ref FlashAttemptEvent args)
    {
        if (ent.Comp.EyesClosed)
            args.Cancel();
    }
}
