using Content.Server.Power.Components;
using Content.Server.Vocalization.Systems;
using Content.Shared.Advertise.Components;

namespace Content.Server._Scp.Misc.AntiSpam;

public sealed class AntiSoundSpamSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnUIClosedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ApcPowerReceiverComponent, TryVocalizeEvent>(CancelEvent);
    }

    private void OnMapInit(Entity<SpeakOnUIClosedComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<ApcPowerReceiverComponent>(ent))
            return;

        RemComp(ent, ent.Comp);
    }

    private static void CancelEvent<T>(T ent, ref TryVocalizeEvent args)
    {
        args.Cancelled = true;
        args.Handled = true;
    }
}
