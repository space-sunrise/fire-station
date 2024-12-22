using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp106.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp106Component : Component
{
    // Если объект сдержан, он не должен иметь возможности юзать способки
    [DataField("isContained")] public bool IsContained = false;
}
