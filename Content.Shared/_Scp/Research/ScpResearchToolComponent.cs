using Content.Shared.DoAfter;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Research;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpResearchToolComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1f;

    [DataField, ViewVariables]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2f);

    [DataField, ViewVariables]
    public string? CooldownMessage;

    [DataField(required: true), NonSerialized]
    public BaseScpSpawnInteractDoAfterEvent Event = default!;

    [DataField, ViewVariables]
    public SoundSpecifier? Sound;

    [DataField, ViewVariables]
    public EntityWhitelist? Whitelist;
}
