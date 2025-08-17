using Content.Client.UserInterface.Systems.Character;
using Content.Shared._Scp.Other.AutoOpenCharacterMenu;
using Robust.Client.Player;
using Robust.Client.UserInterface;

namespace Content.Client._Scp.Other.AutoOpenCharacterMenu;

public sealed class AutoOpenCharacterMenuSystem : SharedAutoOpenCharacterMenuSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<OpenCharacterMenuRequest>(OnOpenRequest);
    }

    private void OnOpenRequest(OpenCharacterMenuRequest ev)
    {
        var playerEntity = GetEntity(ev.Entity);

        if (_player.LocalEntity != playerEntity)
            return;

        _ui.GetUIController<CharacterUIController>().ToggleWindow();
    }
}
