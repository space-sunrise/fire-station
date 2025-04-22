using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Research.Components
{
    [NetSerializable, Serializable]
    public enum ResearchConsoleUiKey : byte
    {
        Key,
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleUnlockTechnologyMessage : BoundUserInterfaceMessage
    {
        public string Id;

        public ConsoleUnlockTechnologyMessage(string id)
        {
            Id = id;
        }
    }

    [Serializable, NetSerializable]
    public sealed class ConsoleServerSelectionMessage : BoundUserInterfaceMessage
    {

    }

    // Fire edit start - сканирование артефактов на расстоянии.
    // Ивент будет сообщать, что при открытии окна нужно поискать на расстоянии таргет, если его нет
    [Serializable, NetSerializable]
    public sealed class ConsoleServerSearchForArtifactInRadius : BoundUserInterfaceMessage;
    // Fire edit end

    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState : BoundUserInterfaceState
    {
        public Dictionary<ProtoId<ResearchPointPrototype>, int> Points;

        public ResearchConsoleBoundInterfaceState(Dictionary<ProtoId<ResearchPointPrototype>, int> points)
        {
            Points = points;
        }
    }
}
