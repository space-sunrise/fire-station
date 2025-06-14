using Content.Server._Sunrise.Helpers;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared._Scp.Scp106.Containment;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server._Scp.Scp106.Containment;

public sealed class Scp106ContainmentSystem : SharedScp106ContainmentSystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MobStateSystem _mobState  = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly SunriseHelpersSystem _helpers  = default!;

    private static readonly SoundSpecifier ContainSound = new SoundPathSpecifier("/Audio/_Scp/scp106_contained_sound.ogg");

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106BoneBreakerCellComponent, StartCollideEvent>(OnBoneBreakerCollide);
    }

    private void OnBoneBreakerCollide(Entity<Scp106BoneBreakerCellComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.OtherEntity))
            return;

        if (_mobState.IsDead(args.OtherEntity))
            return;

        if (!TryContain())
            return;

        _body.GibBody(args.OtherEntity);

        _audio.PlayGlobal(ContainSound, Filter.BroadcastMap(Transform(ent).MapID), true);
        _chat.DispatchStationAnnouncement(ent, Loc.GetString("scp106-return-to-containment"));
    }

    public bool TryContain()
    {
        if (!_helpers.TryGetFirst<Scp106Component>(out var scp106))
            return false;

        if (scp106.Value.Comp.IsContained)
            return false;

        if (!_helpers.TryGetFirst<Scp106ContainmentCatwalkComponent>(out var chamberTile))
            return false;

        var ev = new Scp106RecontainmentAttemptEvent();
        RaiseLocalEvent(scp106.Value, ev);

        if (ev.Cancelled)
            return false;

        var xform = Transform(chamberTile.Value);

        scp106.Value.Comp.IsContained = true;
        Dirty(scp106.Value);

        _transform.SetCoordinates(scp106.Value, xform.Coordinates);

        return true;
    }
}
