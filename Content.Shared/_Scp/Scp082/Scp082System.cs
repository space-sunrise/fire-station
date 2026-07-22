using Content.Shared._Scp.Holding;
using Content.Shared._Scp.Holding.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp082;

public sealed class Scp082System : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp082Component, ScpHoldBreakoutEvent>(OnHoldBreakout);
    }

    private void OnHoldBreakout(Entity<Scp082Component> ent, ref ScpHoldBreakoutEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (!args.WasFullHold)
            return;

        if (!HasComp<ActiveScpHoldableComponent>(ent.Owner))
            return;

        EnsureComp<ScpHoldImmuneComponent>(ent);
        ent.Comp.NextHoldableAttempt = _timing.CurTime + ent.Comp.NextHoldableAttemptDelay;
        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Scp082Component, ScpHoldImmuneComponent>();
        while (query.MoveNext(out var uid, out var scp082, out _))
        {
            if (scp082.NextHoldableAttempt < _timing.CurTime)
                continue;

            RemCompDeferred<ScpHoldImmuneComponent>(uid);
            Dirty(uid, scp082);
        }
    }
}