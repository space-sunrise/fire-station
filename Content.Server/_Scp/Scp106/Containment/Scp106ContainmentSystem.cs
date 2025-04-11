using Content.Server.Chat.Systems;
using Content.Shared._Scp.Scp106.Containment;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server._Scp.Scp106.Containment;

public sealed class Scp106ContainmentSystem : SharedScp106ContainmentSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private readonly SoundSpecifier _containSound = new SoundPathSpecifier("/Audio/_Scp/scp106_contained_sound.ogg");

    protected override bool BoneBreakerCanCollide(Entity<Scp106BoneBreakerCellComponent> ent, ref StartCollideEvent args)
    {
        if (!base.BoneBreakerCanCollide(ent, ref args))
            return false;

        _audio.PlayGlobal(_containSound, Filter.BroadcastMap(Transform(ent).MapID), false);
        _chat.DispatchStationAnnouncement(ent, Loc.GetString("scp106-return-to-containment"));

        return true;
    }

}
