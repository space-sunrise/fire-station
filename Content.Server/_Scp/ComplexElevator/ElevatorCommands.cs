using System.Linq;
using Content.Server._Scp.ComplexElevator;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.Localization;

namespace Content.Server.Administration.Commands
{
    [AdminCommand(AdminFlags.Mapping)]
    public sealed class ElevatorManageFloorsCommand : IConsoleCommand
    {
        [Dependency] private readonly IEntityManager _entManager = default!;

        private Dictionary<string, Entity<ComplexElevatorComponent>> _elevatorCache = new();

        public string Command => "elevator_floors";
        public string Description => Loc.GetString("elevator-manage-floors-desc");
        public string Help => Loc.GetString("elevator-manage-floors-help", ("command", Command));

        public void Execute(IConsoleShell shell, string argStr, string[] args)
        {
            if (args.Length < 2)
            {
                shell.WriteLine(Loc.GetString("elevator-manage-floors-args-error"));
                return;
            }

            var elevatorId = args[0];
            var action = args[1].ToLower();

            UpdateElevatorCache();
            if (!_elevatorCache.TryGetValue(elevatorId, out var elevator))
            {
                shell.WriteError(Loc.GetString("elevator-manage-floors-not-found", ("elevatorId", elevatorId)));
                return;
            }

            switch (action)
            {
                case "add":
                    if (args.Length != 3)
                    {
                        shell.WriteLine(Loc.GetString("elevator-manage-floors-move-help", ("command", Command)));
                        return;
                    }
                    var floorToAdd = args[2];
                    if (elevator.Comp.Floors.Contains(floorToAdd))
                    {
                        shell.WriteError(Loc.GetString("elevator-manage-floors-floor-exists", ("floorName", floorToAdd), ("elevatorId", elevatorId)));
                        return;
                    }
                    elevator.Comp.Floors.Add(floorToAdd);
                    _entManager.Dirty(elevator.Owner, elevator.Comp);
                    shell.WriteLine(Loc.GetString("elevator-manage-floors-added", ("floorName", floorToAdd), ("elevatorId", elevatorId), ("floors", string.Join(", ", elevator.Comp.Floors))));
                    break;

                case "remove":
                    if (args.Length != 3)
                    {
                        shell.WriteLine(Loc.GetString("elevator-manage-floors-move-help", ("command", Command)));
                        return;
                    }
                    var floorToRemove = args[2];
                    if (!elevator.Comp.Floors.Contains(floorToRemove))
                    {
                        shell.WriteError(Loc.GetString("elevator-manage-floors-floor-not-exists", ("floorName", floorToRemove), ("elevatorId", elevatorId)));
                        return;
                    }
                    if (elevator.Comp.CurrentFloor == floorToRemove)
                    {
                        shell.WriteError(Loc.GetString("elevator-manage-floors-cannot-remove-current", ("floorName", floorToRemove), ("elevatorId", elevatorId)));
                        return;
                    }
                    elevator.Comp.Floors.Remove(floorToRemove);
                    _entManager.Dirty(elevator.Owner, elevator.Comp);
                    shell.WriteLine(Loc.GetString("elevator-manage-floors-removed", ("floorName", floorToRemove), ("elevatorId", elevatorId), ("floors", string.Join(", ", elevator.Comp.Floors))));
                    break;

                case "list":
                    if (args.Length != 2)
                    {
                        shell.WriteLine(Loc.GetString("elevator-manage-floors-move-help", ("command", Command)));
                        return;
                    }
                    shell.WriteLine(Loc.GetString("elevator-manage-floors-list", ("elevatorId", elevatorId), ("floors", string.Join(", ", elevator.Comp.Floors)), ("currentFloor", elevator.Comp.CurrentFloor)));
                    break;

                case "move":
                    if (args.Length != 4)
                    {
                        shell.WriteLine(Loc.GetString("elevator-manage-floors-move-help", ("command", Command)));
                        return;
                    }
                    var floorToMove = args[2];
                    var direction = args[3];
                    var floors = elevator.Comp.Floors;
                    var currentIndex = floors.IndexOf(floorToMove);
                    if (currentIndex == -1)
                    {
                        shell.WriteError(Loc.GetString("elevator-manage-floors-floor-not-exists", ("floorName", floorToMove), ("elevatorId", elevatorId)));
                        return;
                    }
                    int newIndex;
                    if (direction == "up")
                    {
                        if (currentIndex == 0)
                        {
                            shell.WriteError(Loc.GetString("elevator-manage-floors-already-top", ("floorName", floorToMove)));
                            return;
                        }
                        newIndex = currentIndex - 1;
                    }
                    else if (direction == "down")
                    {
                        if (currentIndex == floors.Count - 1)
                        {
                            shell.WriteError(Loc.GetString("elevator-manage-floors-already-bottom", ("floorName", floorToMove)));
                            return;
                        }
                        newIndex = currentIndex + 1;
                    }
                    else if (int.TryParse(direction, out newIndex))
                    {
                        if (newIndex < 0 || newIndex >= floors.Count)
                        {
                            shell.WriteError(Loc.GetString("elevator-manage-floors-invalid-index", ("index", newIndex), ("maxIndex", floors.Count - 1)));
                            return;
                        }
                    }
                    else
                    {
                        shell.WriteError(Loc.GetString("elevator-manage-floors-unknown-direction", ("direction", direction)));
                        return;
                    }
                    floors.RemoveAt(currentIndex);
                    floors.Insert(newIndex, floorToMove);
                    _entManager.Dirty(elevator.Owner, elevator.Comp);
                    shell.WriteLine(Loc.GetString("elevator-manage-floors-moved", ("floorName", floorToMove), ("newIndex", newIndex), ("floors", string.Join(", ", floors))));
                    break;

                default:
                    shell.WriteError(Loc.GetString("elevator-manage-floors-unknown-action", ("action", action)));
                    break;
            }
        }

        private void UpdateElevatorCache()
        {
            _elevatorCache.Clear();
            var query = _entManager.AllEntityQueryEnumerator<ComplexElevatorComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                _elevatorCache[comp.ElevatorId] = (uid, comp);
            }
        }

        public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
        {
            if (args.Length == 1)
            {
                UpdateElevatorCache();
                var elevators = _elevatorCache.Keys;
                return CompletionResult.FromHintOptions(elevators, "<elevator_id>");
            }
            if (args.Length == 2)
            {
                var actions = new[] { "add", "remove", "move", "list" };
                return CompletionResult.FromHintOptions(actions, "<action>");
            }
            return CompletionResult.Empty;
        }
    }
}