using System.Linq;
using Content.Server._Scp.Scp106.Components;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._Scp.Scp106.Components;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Humanoid;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Server._Scp.Scp106.Systems;

public sealed class Scp106CatwalkSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly PhysicsSystem _physics = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;

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
            scp106Component.IsContained = true;
            _body.GibBody(uid);
            _transform.SetCoordinates(scp106Uid, Transform(containerUidPicked).Coordinates);
            _chat.DispatchStationAnnouncement(containerUidPicked, Loc.GetString("scp106-return-to-containment"));
            // _audio.PlayGlobal("/Audio/_Scp/scp106_contained_sound.ogg", Filter.Broadcast(), false); не знаю почему не работает
            break;
        }
    }

    private void OnTargetUnstood(Entity<Scp106CatwalkTargetComponent> ent, ref EndCollideEvent args) { }

    private void OnContainerStood(Entity<Scp106CatwalkContainerComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        scp106Component.IsContained = true;
        EnsureComp<PacifiedComponent>(args.OtherEntity);

        if (!TryComp<FixturesComponent>(args.OtherEntity, out var fixturesComponent))
            return;

        foreach (var (id, fixture) in fixturesComponent.Fixtures)
        {
            _physics.SetCollisionMask(args.OtherEntity, id, fixture, 30);

        }
    }

    private void OnContainerUnstood(Entity<Scp106CatwalkContainerComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        // Проверка, стоит ли SCP-106 всё ещё на другом контейнере
        var isStillOnContainer = _lookup.GetEntitiesInRange(Transform(args.OtherEntity).Coordinates, 0.1f)
            .Any(e => HasComp<Scp106CatwalkContainerComponent>(e));

        if (!isStillOnContainer) // Если он не на другом контейнере, сбрасываем состояние
        {
            scp106Component.IsContained = false;
            RemComp<PacifiedComponent>(args.OtherEntity);


            if (!TryComp<FixturesComponent>(args.OtherEntity, out var fixturesComponent))
                return;

            foreach (var (id, fixture) in fixturesComponent.Fixtures)
            {
                _physics.SetCollisionMask(args.OtherEntity, id, fixture, 18);

            }
        }

    }
}
