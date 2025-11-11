using Content.Shared.Buckle.Components;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Item;
using Content.Shared.Pulling.Events;
using Content.Shared.Speech;

namespace Content.Shared._Scp.Scp999;

public abstract class SharedScp999System : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp999Component, CanSeeAttemptEvent>(OnCanSee);
        SubscribeLocalEvent<Scp999Component, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<Scp999Component, GettingPickedUpAttemptEvent>(OnPickupAttempt);
        SubscribeLocalEvent<Scp999Component, BeingPulledAttemptEvent>(OnBeingPulledAttempt);
        SubscribeLocalEvent<Scp999Component, StartPullAttemptEvent>(OnStartPullAttempt);
        SubscribeLocalEvent<Scp999Component, BuckleAttemptEvent>(OnBuckleAttempt);

        SubscribeLocalEvent<Scp999Component, ExaminedEvent>(OnExamined);

    }

    private static void OnCanSee(Entity<Scp999Component> entity, ref CanSeeAttemptEvent args)
    {
        if (entity.Comp.CurrentState == Scp999States.Rest)
            args.Cancel();
    }

    private static void OnSpeakAttempt(Entity<Scp999Component> entity, ref SpeakAttemptEvent args)
    {
        if (entity.Comp.CurrentState == Scp999States.Rest)
            args.Cancel();
    }

    private static void OnPickupAttempt(Entity<Scp999Component> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (ent.Comp.CurrentState == Scp999States.Wall)
            args.Cancel();
    }

    private static void OnBeingPulledAttempt(Entity<Scp999Component> ent, ref BeingPulledAttemptEvent args)
    {
        if (ent.Comp.CurrentState != Scp999States.Default)
            args.Cancel();
    }

    private static void OnStartPullAttempt(Entity<Scp999Component> ent, ref StartPullAttemptEvent args)
    {
        if (ent.Comp.CurrentState != Scp999States.Default)
            args.Cancel();
    }

    private static void OnBuckleAttempt(Entity<Scp999Component> ent, ref BuckleAttemptEvent args)
    {
        if (ent.Comp.CurrentState == Scp999States.Default)
            return;

        args.Cancelled = true;
    }

    private void OnExamined(Entity<Scp999Component> entity, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (entity.Comp.CurrentState != Scp999States.Rest)
            return;

        args.PushMarkup(Loc.GetString("sleep-examined", ("target", Identity.Entity(entity, EntityManager))));
    }
}
