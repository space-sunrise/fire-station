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
        SubscribeLocalEvent<Scp106CatwalkCatcherComponent, StartCollideEvent>(OnCatcherStood);
        SubscribeLocalEvent<Scp106CatwalkCatcherComponent, EndCollideEvent>(OnCatcherUnstood);
    }

    private void OnTargetStood(Entity<Scp106CatwalkTargetComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(args.OtherEntity, out var humanoidAppearanceComponent) ||
            ent.Comp.StandingEnt != null)
            return;
        ent.Comp.StandingEnt = args.OtherEntity;
    }

    private void OnTargetUnstood(Entity<Scp106CatwalkTargetComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.StandingEnt == null)
            return;
        if (args.OtherEntity == ent.Comp.StandingEnt)
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

    private void OnCatcherStood(Entity<Scp106CatwalkCatcherComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var humanoidAppearanceComponent) ||
            ent.Comp.StandingEnt != null)
            return;
        ent.Comp.StandingEnt = args.OtherEntity;
    }

    private void OnCatcherUnstood(Entity<Scp106CatwalkCatcherComponent> ent, ref EndCollideEvent args)
    {
        if (ent.Comp.StandingEnt == null)
            return;
        if (args.OtherEntity == ent.Comp.StandingEnt)
            ent.Comp.StandingEnt = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var catcherQuery = AllEntityQuery<Scp106CatwalkCatcherComponent>();
        while (catcherQuery.MoveNext(out var catcherUid, out var catcherComponent))
        {
            if (catcherComponent.StandingEnt == null)
                continue;
            var targetQuery = AllEntityQuery<Scp106CatwalkTargetComponent>();
            while (targetQuery.MoveNext(out var targetUid, out var targetComponent))
            {
                if (targetComponent.StandingEnt == null)
                    continue;

                // Поймали пару. Производим телепорт
                _body.GibBody(targetComponent.StandingEnt.Value);
                _transform.SetCoordinates(catcherComponent.StandingEnt.Value, Transform(targetUid).Coordinates);
                _chat.DispatchStationAnnouncement(targetUid, Loc.GetString("scp106-return-to-containment"));
                break;
            }
        }
    }
}
