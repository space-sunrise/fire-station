using Content.Shared._Scp.Scp106;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Scp106.Systems;

public sealed partial class Scp106System
{
    private static readonly EntProtoId PhantomRemains = "Ash";

    private void OnMobStateChangedEvent(Entity<Scp106PhantomComponent> ent, ref MobStateChangedEvent args)
    {
        if (!_mobState.IsIncapacitated(ent))
            return;

        Spawn(PhantomRemains, Transform(ent).Coordinates);
        QueueDel(ent);

        if (!_mind.TryGetMind(ent, out var mindId, out _))
            return;

        if (!Exists(ent.Comp.Scp106BodyUid))
            return;

        _mind.TransferTo(mindId, ent.Comp.Scp106BodyUid);
    }

    private void OnScp106ReverseActionEvent(Entity<Scp106PhantomComponent> ent, ref Scp106ReverseActionEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;

        if (!HasComp<HumanoidAppearanceComponent>(target))
            return;

        if (!_mobState.IsDead(target))
            return;

        if (!Exists(ent.Comp.Scp106BodyUid))
            return;

        var targetPos = Transform(target).Coordinates;

        _transform.SetCoordinates(ent.Comp.Scp106BodyUid.Value, targetPos);
        SendToBackrooms(target);

        if (args.Args.EventTarget == null)
            return;

        _mobState.ChangeMobState(args.Args.EventTarget.Value, MobState.Dead);
    }

    public override void BecomeTeleportPhantom(EntityUid uid, ref Scp106BecomeTeleportPhantomAction args)
    {
        if (!_mind.TryGetMind(uid, out var mindId, out _))
            return;

        var phantom = Spawn(args.PhantomPrototype, Transform(uid).Coordinates);

        _mind.TransferTo(mindId, phantom);

        if (TryComp<Scp106PhantomComponent>(phantom, out var scp106PhantomComponent))
            scp106PhantomComponent.Scp106BodyUid = uid;

        var doAfterEventArgs = new DoAfterArgs(EntityManager, uid, args.Delay, new Scp106BecomeTeleportPhantomActionEvent(), phantom)
        {
            BreakOnMove = false,
            BreakOnDamage = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterEventArgs))
            return;

        _appearance.SetData(uid, Scp106Visuals.Visuals, Scp106VisualsState.Entering);
    }

    public override void BecomePhantom(Entity<Scp106Component> ent, ref Scp106BecomePhantomAction args)
    {
        var scp106Phantom = Spawn(args.PhantomPrototype, Transform(ent).Coordinates);

        if (_mind.TryGetMind(ent, out var mindId, out _))
            _mind.TransferTo(mindId, scp106Phantom);

        if (!TryComp<Scp106PhantomComponent>(scp106Phantom, out var scp106PhantomComponent))
            return;

        scp106PhantomComponent.Scp106BodyUid = ent;
        Dirty(ent);

        args.Action.Comp.UseDelay = ent.Comp.PhantomCoolDown;
        args.Handled = true;
    }
}
