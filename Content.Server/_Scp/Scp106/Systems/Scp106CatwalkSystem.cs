using System.Linq;
using Content.Server._Scp.Scp106.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Humanoid;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Events;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106CatwalkSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<Scp106CatwalkTargetComponent, StartCollideEvent>(OnTargetStood);
        SubscribeLocalEvent<Scp106CatwalkTargetComponent, EndCollideEvent>(OnTargetUnstood);

        SubscribeLocalEvent<Scp106CatwalkContainerComponent, StartCollideEvent>(OnContainerStood);
        SubscribeLocalEvent<Scp106CatwalkContainerComponent, EndCollideEvent>(OnContainerUnstood);
    }

    private void OnTargetStood(Entity<Scp106CatwalkTargetComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.OtherEntity))
            return;

        ContainScp106(args.OtherEntity);
    }

    private void ContainScp106(EntityUid uid)
    {
        var scp106Query = AllEntityQuery<Scp106Component>();

        while (scp106Query.MoveNext(out var scp106Uid, out var scp106Component))
        {
            if (scp106Component.IsContained)
                continue;

            var containerUidPicked = EntityQuery<Scp106CatwalkContainerComponent>().First().Owner;

            _body.GibBody(uid);
            _transform.SetCoordinates(scp106Uid, Transform(containerUidPicked).Coordinates);
            _chat.DispatchStationAnnouncement(containerUidPicked, Loc.GetString("scp106-return-to-containment"));

            break;
        }
    }

    private void OnTargetUnstood(Entity<Scp106CatwalkTargetComponent> ent, ref EndCollideEvent args) { }

    private void OnContainerStood(Entity<Scp106CatwalkContainerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        scp106Component.IsContained = true;
    }

    private void OnContainerUnstood(Entity<Scp106CatwalkContainerComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        scp106Component.IsContained = false;
    }
}
