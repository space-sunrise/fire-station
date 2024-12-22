using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Backrooms.EmitEmotesPeriodically;

[RegisterComponent]
public sealed partial class EmitEmotesPeriodicallyComponent : Component
{
    [DataField(required: true), ViewVariables]
    public HashSet<ProtoId<EmotePrototype>> Emotes = new();

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EmitMode Mode = EmitMode.All;

    #region Timings

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(30);

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int CooldownVariations;

    [ViewVariables]
    public TimeSpan CooldownAddition = TimeSpan.Zero;

    [ViewVariables]
    public TimeSpan LastTimeEmit = TimeSpan.Zero;

    #endregion
}

public enum EmitMode : byte
{
    All,
    Random
}

