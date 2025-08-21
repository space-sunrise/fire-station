using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Speech.Muting
{
    [RegisterComponent, NetworkedComponent]
    public sealed partial class MutedComponent : Component
    {
        // Fire edit start
        [DataField]
        public HashSet<ProtoId<EmotePrototype>> Allowed = [];
        // Fire edit end
    }
}
