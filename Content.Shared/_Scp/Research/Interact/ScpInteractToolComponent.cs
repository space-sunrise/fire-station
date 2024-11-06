using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Research.Interact;

[RegisterComponent, NetworkedComponent]
public sealed partial class ScpInteractToolComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Delay = 1f;

    [DataField, ViewVariables]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(2f);

    [DataField, ViewVariables]
    public string? CooldownMessage;

    [DataField(required: true), NonSerialized]
    public ScpSpawnInteractDoAfterEvent Event = default!;

    [DataField, ViewVariables]
    public SoundSpecifier? Sound;

    [DataField, ViewVariables]
    public EntityWhitelist? Whitelist;
}
