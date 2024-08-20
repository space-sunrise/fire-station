using Content.Shared.CombatMode.Pacification;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Pulling.Events;
using Robust.Shared.Physics.Events;

namespace Content.Shared._Scp.Scp096;


public abstract class SharedScp096System : EntitySystem
{
    [Dependency] private readonly SharedDoorSystem _doorSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096Component, PullAttemptEvent>(OnPullAttempt);
        SubscribeLocalEvent<Scp096Component, AttemptPacifiedAttackEvent>(OnPacifiedAttackAttempt);
        SubscribeLocalEvent<Scp096Component, AttackAttemptEvent>(OnAttackAttempt);
        SubscribeLocalEvent<Scp096Component, StartCollideEvent>(OnCollide);
    }

    protected virtual void OnCollide(Entity<Scp096Component> ent, ref StartCollideEvent args)
    {
        if(TryComp<DoorComponent>(args.OtherEntity, out var doorComponent))
        {
            HandleDoorCollision(ent, new Entity<DoorComponent>(args.OtherEntity, doorComponent));
        }
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
