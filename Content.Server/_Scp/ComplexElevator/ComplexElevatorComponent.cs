using System.Collections.Generic;
using Robust.Shared.Audio;

namespace Content.Server._Scp.ComplexElevator;

[RegisterComponent]
public sealed partial class ComplexElevatorComponent : Component
{
    [DataField]
    public string ElevatorId = string.Empty;

    [DataField]
    public string CurrentFloor = "IntermediateFloor";

    [DataField]
    public List<string> Floors = new();

    [DataField]
    public string IntermediateFloorId = "IntermediateFloor";

    [DataField]
    public TimeSpan SendDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan IntermediateDelay = TimeSpan.FromSeconds(6);

    [DataField]
    public SoundSpecifier? StartSound;

    [DataField]
    public SoundSpecifier? TravelSound;

    [DataField]
    public SoundSpecifier? ArrivalSound;

    public bool IsMoving = false;
}