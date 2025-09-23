using System.Numerics;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.MetaGarbage;

[RegisterComponent]
public sealed partial class MetaGarbageTargetComponent : Component
{
    /// <summary>
    /// Какое процентное соотношение от общего числа собранного в прошлом раунде мусора вернется в новом раунде?
    /// </summary>
    [DataField]
    public float SpawnPercent = 70f;
}

public record struct StationMetaGarbageData(EntProtoId Prototype, Vector2 Position, Angle Rotation)
{
    public EntProtoId Prototype = Prototype;
    public Vector2 Position = Position;
    public Angle Rotation = Rotation;
}
