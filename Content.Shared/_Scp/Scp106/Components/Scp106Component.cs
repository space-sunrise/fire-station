using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp106.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class Scp106Component : Component
{
    /// <summary>
    /// Если объект сдержан, он не должен иметь возможности юзать способки
    /// TODO: Возможно переместить в <see cref="ScpComponent"/>
    /// </summary>
    [DataField] public bool IsContained;

    [DataField] public int AmoutOfPhantoms = 3;

    public float Accumulator = 0;
}
