using System.Linq;
using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Scp106.Components;
/// using Content.Shared.Body.Systems;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Systems;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp106.Containment;

public abstract class SharedScp106ContainmentSystem : EntitySystem
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly MobStateSystem _mobState  = default!;
    [Dependency] private readonly SharedScpHelpersSystem _scpHelpers = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<Scp106BoneBreakerCellComponent, StartCollideEvent>(OnBoneBreakerCollide);

        SubscribeLocalEvent<Scp106ContainmentCatwalkComponent, StartCollideEvent>(OnContainmentCatwalkCollideStart);
        SubscribeLocalEvent<Scp106ContainmentCatwalkComponent, EndCollideEvent>(OnContainmentCatwalkCollideEnd);
    }

    private void OnBoneBreakerCollide(Entity<Scp106BoneBreakerCellComponent> ent, ref StartCollideEvent args)
    {
        BoneBreakerCanCollide(ent, ref args);
    }

    protected virtual bool BoneBreakerCanCollide(Entity<Scp106BoneBreakerCellComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<HumanoidAppearanceComponent>(args.OtherEntity))
            return false;

        if (_mobState.IsDead(args.OtherEntity))
            return false;

        if (!TryContain())
            return false;

        var damage = new DamageSpecifier(_prototypeManager.Index<DamageTypePrototype>("Slash"), 30);
        _damageableSystem.TryChangeDamage(args.OtherEntity, damage);

        if (!_scpHelpers.TryGetFirst<Scp106ContainmentCatwalkComponent>(out var chamberTile))
            return false;

        var xform = Transform(chamberTile.Value);

        _transform.SetCoordinates(args.OtherEntity, xform.Coordinates);
        // Аннонс в сервер-сайд системе

        return true;
    }

    private bool TryContain()
    {
        if (!_scpHelpers.TryGetFirst<Scp106Component>(out var scp106))
            return false;

        if (!_scpHelpers.TryGetFirst<Scp106ContainmentCatwalkComponent>(out var chamberTile))
            return false;

        var xform = Transform(chamberTile.Value);

        scp106.Value.Comp.IsContained = true;
        Dirty(scp106.Value);

        _transform.SetCoordinates(scp106.Value, xform.Coordinates);

        return true;
    }

    private void OnContainmentCatwalkCollideStart(Entity<Scp106ContainmentCatwalkComponent> ent, ref StartCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        var mapUid = Transform(args.OtherEntity).MapUid;

        if (HasComp<Scp106DimensionShiftingMapComponent>(mapUid))
            return;

        scp106Component.IsContained = true;
        Dirty(ent);

        if (!TryComp<FixturesComponent>(args.OtherEntity, out var fixturesComponent))
            return;

        foreach (var (id, fixture) in fixturesComponent.Fixtures)
        {
            _physics.SetCollisionMask(args.OtherEntity, id, fixture, 30);
        }
    }

    private void OnContainmentCatwalkCollideEnd(Entity<Scp106ContainmentCatwalkComponent> ent, ref EndCollideEvent args)
    {
        if (!TryComp<Scp106Component>(args.OtherEntity, out var scp106Component))
            return;

        // Проверка, стоит ли SCP-106 всё ещё на другом контейнере
        var isStillOnContainer = _lookup.GetEntitiesInRange(Transform(args.OtherEntity).Coordinates, 0.1f)
            .Any(HasComp<Scp106ContainmentCatwalkComponent>);

        if (isStillOnContainer)
            return;

        // Если он не на другом контейнере, сбрасываем состояние
        scp106Component.IsContained = false;
        Dirty(args.OtherEntity, scp106Component);

        if (!TryComp<FixturesComponent>(args.OtherEntity, out var fixturesComponent))
            return;

        foreach (var (id, fixture) in fixturesComponent.Fixtures)
        {
            _physics.SetCollisionMask(args.OtherEntity, id, fixture, 18);
        }
    }
}
