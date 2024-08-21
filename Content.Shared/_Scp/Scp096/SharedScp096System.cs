using Content.Shared.CombatMode.Pacification;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096;


public abstract class SharedScp096System : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<Scp096Component, AttemptPacifiedAttackEvent>(OnPacifiedAttackAttempt);
        SubscribeLocalEvent<Scp096Component, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp096Component, StartCollideEvent>(OnCollide);
    }

    protected void OnCollide(Entity<Scp096Component> ent, ref StartCollideEvent args)
    {
        if(TryComp<DoorComponent>(args.OtherEntity, out var doorComponent))
        {
            HandleDoorCollision(ent, new Entity<DoorComponent>(args.OtherEntity, doorComponent));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<Scp096Component>();

        while (query.MoveNext(out var scpUid, out var scp096Component))
        {
            var scpEntity = new Entity<Scp096Component>(scpUid, scp096Component);
            UpdateScp096(scpEntity);
        }
    }

    protected virtual void UpdateScp096(Entity<Scp096Component> scpEntity)
    {
        if (scpEntity.Comp.Pacified || !scpEntity.Comp.InRageMode || !scpEntity.Comp.RageStartTime.HasValue)
            return;

        var currentTime = _gameTiming.CurTime;
        var elapsedTime = currentTime - scpEntity.Comp.RageStartTime.Value;

        if (elapsedTime.TotalSeconds > scpEntity.Comp.RageDuration)
        {
            OnRageTimeExceeded(scpEntity);
        }
    }

    protected virtual void OnRageTimeExceeded(Entity<Scp096Component> scpEntity)
    {

    }

    protected virtual void HandleDoorCollision(Entity<Scp096Component> scpEntity, Entity<DoorComponent> doorEntity)
    {
        if (!scpEntity.Comp.InRageMode)
        {
            return;
        }

        _doorSystem.StartOpening(doorEntity);
    }

    private void OnAttackAttempt(Entity<Scp096Component> ent, ref AttackAttemptEvent args)
    {
        if (!args.Target.HasValue)
        {
            return;
        }

        if (!TryComp<Scp096TargetComponent>(args.Target.Value, out var targetComponent)
            || !targetComponent.TargetedBy.Contains(ent.Owner))
        {
            args.Cancel();
        }
    }

    private void OnPacifiedAttackAttempt(Entity<Scp096Component> ent, ref AttemptPacifiedAttackEvent args)
    {
        args.Reason = Loc.GetString("scp096-non-argo-attack-attempt");
        args.Cancelled = true;
    }

    private void OnPullAttempt(Entity<Scp096Component> ent, ref PullAttemptEvent args)
    {
        if (!ent.Comp.Pacified)
        {
            args.Cancelled = true;
        }
    }

}
