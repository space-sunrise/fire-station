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

    // Fire edit start - поддержка разных видов очков исследований
    [Serializable, NetSerializable]
    public sealed class ResearchConsoleBoundInterfaceState(Dictionary<ProtoId<ResearchPointPrototype>, int> points, TimeSpan nextRediscover, Dictionary<ProtoId<ResearchPointPrototype>, int> rediscoverCost) : BoundUserInterfaceState
    {
        public Dictionary<ProtoId<ResearchPointPrototype>, int> Points = points;

        public TimeSpan NextRediscover = nextRediscover;

        public Dictionary<ProtoId<ResearchPointPrototype>, int> RediscoverCost = rediscoverCost;
    }
    // Fire edit end
}
