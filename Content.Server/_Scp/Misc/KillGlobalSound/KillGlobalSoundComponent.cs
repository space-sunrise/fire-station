using Content.Shared.Whitelist;
using Robust.Shared.Audio;

namespace Content.Server._Scp.Misc.KillGlobalSound;

[RegisterComponent]
public sealed partial class KillGlobalSoundComponent : Component
{
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    [DataField(required: true)]
    public EntityWhitelist OriginWhitelist = default!;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float Chance = 1f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float MaxRadius = 30f;
}
