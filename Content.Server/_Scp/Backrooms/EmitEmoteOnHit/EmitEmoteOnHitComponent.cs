using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Backrooms.EmitEmoteOnHit;

[RegisterComponent]
public sealed partial class EmitEmoteOnHitComponent : Component
{
    [DataField(required: true), ViewVariables]
    public ProtoId<EmotePrototype> Emote;
}
