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

        if (ent.Comp.StandingEnt != null)
            return;

        ent.Comp.StandingEnt = args.OtherEntity;

        ContainScp106(args.OtherEntity);
    }

    private void ContainScp106(EntityUid uid)
    {
        var scp106Query = AllEntityQuery<Scp106Component>();

        while (scp106Query.MoveNext(out var scp106Uid, out var scp106Component))
        {
            if (!scp106Component.IsContained)
                continue;

            var containerUidPicked = EntityQuery<Scp106CatwalkContainerComponent>().First().Owner;

            _body.GibBody(uid);
            _transform.SetCoordinates(scp106Uid, Transform(containerUidPicked).Coordinates);
            _chat.DispatchStationAnnouncement(containerUidPicked, Loc.GetString("scp106-return-to-containment"));

            break;
        }
    }

    private void OnTargetUnstood(Entity<Scp106CatwalkTargetComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.StandingEnt == null)
            return;

        if (args.OtherEntity != ent.Comp.StandingEnt)
            return;

        ent.Comp.StandingEnt = null;

        // Выбираем следующую цель
        var lookup = _lookup.GetEntitiesIntersecting(ent.Owner);
        foreach (var standingUid in lookup)
        {
            if (!HasComp<HumanoidAppearanceComponent>(standingUid))
                continue;

            ent.Comp.StandingEnt = standingUid;
            break;
        }
    }

    private void OnContainerStood(Entity<Scp106CatwalkContainerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        if (ent.Comp.StandingEnt != null)
            return;

        scp106Component.IsContained = true;

        ent.Comp.StandingEnt = args.OtherEntity;

    }

    private void OnContainerUnstood(Entity<Scp106CatwalkContainerComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.StandingEnt == null)
            return;

        if (args.OtherEntity != ent.Comp.StandingEnt)
            return;

        ent.Comp.StandingEnt = null;
    }
}
