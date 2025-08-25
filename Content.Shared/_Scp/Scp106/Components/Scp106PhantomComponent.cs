using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp106.Components;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp106PhantomComponent : Component
{
    // Тут хранится айдишник тела 106, чтобы он мог вернуться в него из состояния фантома
    [AutoNetworkedField]
    public EntityUid? Scp106BodyUid;

    [DataField]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/_Scp/Scp106/phantom_killed.ogg");
}
