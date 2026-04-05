using Content.Shared.Whitelist;

namespace Content.Shared.Radio.Components;

public sealed partial class EncryptionKeyHolderComponent
{
    /// <summary>
    ///     Encryption keys that may be inserted into this holder.
    /// </summary>
    [DataField]
    public EntityWhitelist? KeyWhitelist;

    /// <summary>
    ///     Encryption keys that cannot be inserted into this holder.
    /// </summary>
    [DataField]
    public EntityWhitelist? KeyBlacklist;
}
