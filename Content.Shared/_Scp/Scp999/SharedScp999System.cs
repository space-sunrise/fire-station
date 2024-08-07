using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Speech;

namespace Content.Shared._Scp.Scp999;

public abstract class SharedScp999System : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp999Component, CanSeeAttemptEvent>(OnCanSee);
        SubscribeLocalEvent<Scp999Component, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<Scp999Component, ExaminedEvent>(OnExamined);
    }

    private void OnCanSee(EntityUid uid, Scp999Component component, ref CanSeeAttemptEvent args)
    {
        if (component.CurrentState == Scp999States.Rest)
        {
            args.Cancel();
        }
    }

    private void OnSpeakAttempt(EntityUid uid, Scp999Component component, ref SpeakAttemptEvent args)
    {
        if (component.CurrentState == Scp999States.Rest)
        {
            args.Cancel();
        }
    }

    private void OnExamined(EntityUid uid, Scp999Component component, ref ExaminedEvent args)
    {
        if (args.IsInDetailsRange)
        {
            args.PushMarkup(Loc.GetString("sleep-examined", ("target", Identity.Entity(uid, EntityManager))));
        }
    }
}
