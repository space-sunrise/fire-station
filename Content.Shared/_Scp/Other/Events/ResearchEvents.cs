using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Other.Events;

[Serializable, NetSerializable]
public sealed class ConsoleServerSearchForArtifactInRadius : BoundUserInterfaceMessage;
