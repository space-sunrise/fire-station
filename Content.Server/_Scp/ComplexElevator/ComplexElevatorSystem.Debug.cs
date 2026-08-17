using Content.Server.Administration.Managers;
using Content.Shared._Scp.ComplexElevator;
using Content.Shared.Database;
using Content.Shared.Verbs;
using Robust.Server.GameObjects;
using Robust.Shared.Player;

namespace Content.Server._Scp.ComplexElevator;

public sealed partial class ComplexElevatorSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;

    private void InitializeDebug()
    {
        SubscribeLocalEvent<ComplexElevatorComponent, GetVerbsEvent<Verb>>(AddAdminElevatorVerbs);
        SubscribeLocalEvent<ElevatorPointComponent, GetVerbsEvent<Verb>>(AddAdminPointVerbs);
    }

    private void AddAdminElevatorVerbs(EntityUid uid, ComplexElevatorComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor) || !_adminManager.IsAdmin(actor.PlayerSession))
            return;

        foreach (var floor in comp.Floors)
        {
            var targetFloor = floor;
            args.Verbs.Add(new Verb
            {
                Text = $"Send to: {targetFloor}",
                Category = VerbCategory.Debug,
                Act = () => MoveToFloor((uid, comp), targetFloor),
                Impact = LogImpact.Medium
            });
        }
    }

    private void AddAdminPointVerbs(EntityUid uid, ElevatorPointComponent comp, GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor) || !_adminManager.IsAdmin(actor.PlayerSession))
            return;

        foreach (var (elevatorUid, elevatorComp) in _elevatorIndex)
        {
            if (!TryComp<ComplexElevatorComponent>(elevatorComp, out var elevator))
                continue;

            if (!elevator.Floors.Contains(comp.FloorId))
                continue;

            var elUid = elevatorComp;
            var floorId = comp.FloorId;

            args.Verbs.Add(new Verb
            {
                Text = $"[Elevator: {elevator.ElevatorId}] Send here",
                Category = VerbCategory.Debug,
                Act = () => MoveToFloor((elUid, elevator), floorId),
                Impact = LogImpact.Medium
            });

            args.Verbs.Add(new Verb
            {
                Text = $"[Elevator: {elevator.ElevatorId}] Teleport here instantly",
                Category = VerbCategory.Debug,
                Act = () =>
                {
                    ClearGasInTargetArea(elUid, floorId);
                    KillEntitiesInTargetArea((elUid, elevator), floorId);
                    elevator.CurrentFloor = floorId;
                    Dirty(elUid, elevator);
                    TeleportToFloor(elUid, floorId, elevator);
                },
                Impact = LogImpact.High
            });
        }
    }
}
