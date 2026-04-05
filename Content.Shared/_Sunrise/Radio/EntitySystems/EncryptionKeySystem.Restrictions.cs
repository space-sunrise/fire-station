using Content.Shared.Radio.Components;
using Content.Shared.Whitelist;

namespace Content.Shared.Radio.EntitySystems;

public sealed partial class EncryptionKeySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private bool IsKeyInsertBlocked(EntityUid holder, EntityUid key, EncryptionKeyHolderComponent component, EntityUid user)
    {
        if (_whitelist.CheckBoth(key, component.KeyBlacklist, component.KeyWhitelist))
            return false;

        _popup.PopupPredictedCursor(Loc.GetString("encryption-key-does-not-fit"), user);
        return true;
    }
}
