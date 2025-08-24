using Content.Server.Power.Components;
using Content.Shared.Advertise.Components;

namespace Content.Server._Scp.Misc.AntiSpam;

public sealed class AntiSoundSpamSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpeakOnUIClosedComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<SpeakOnUIClosedComponent> ent, ref MapInitEvent args)
    {
        if (!HasComp<ApcPowerReceiverComponent>(ent))
            return;

        RemComp(ent, ent.Comp);
    }
}
