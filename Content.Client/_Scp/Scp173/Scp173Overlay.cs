using System.Numerics;
using Content.Client.Actions;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Physics;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Scp173;

public sealed class Scp173Overlay : Overlay
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IInputManager _inputManager = default!;
    [Dependency] private readonly IEyeManager _eyeManager = default!;
    [Dependency] private readonly IResourceCache _resource = default!;

    private readonly SharedTransformSystem _transform;
    private readonly ActionUIController _controller;
    private readonly ActionsSystem _actionsSystem;
    private readonly SharedPhysicsSystem _physics;
    private readonly ExamineSystemShared _examine;

    public Scp173Overlay(SharedTransformSystem transform,
        ActionUIController controller,
        ActionsSystem actionsSystem,
        SharedPhysicsSystem physics,
        ExamineSystemShared examine)
    {
        IoCManager.InjectDependencies(this);
        _transform = transform;
        _controller = controller;
        _actionsSystem = actionsSystem;
        _physics = physics;
        _examine = examine;
    }

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    protected override void Draw(in OverlayDrawArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;
        if (playerEntity == null)
            return;

        // if (_controller.SelectingTargetFor is not { } actionId)
        //     return;
        //
        // if (!_actionsSystem.TryGetActionData(actionId, out var baseAction) ||
        //     baseAction is not WorldTargetActionComponent action)
        //     return;
        //
        // if (!action.Enabled
        //     || action is { Charges: 0, RenewCharges: false }
        //     || action.Cooldown.HasValue && action.Cooldown.Value.End > _timing.CurTime)
        //     return;

        var mousePos = _eyeManager.PixelToMap(_inputManager.MouseScreenPosition);
        if (mousePos.MapId == MapId.Nullspace)
            return;

        var playerPos = _transform.GetWorldPosition(playerEntity.Value);

        if (!_examine.InRangeUnOccluded(
                mousePos,
                _transform.GetMapCoordinates(playerEntity.Value),
                ExamineSystemShared.MaxRaycastRange,
                null))
            return;

        args.WorldHandle.DrawLine(playerPos, mousePos.Position, Color.Aqua);
        args.WorldHandle.DrawCircle(mousePos.Position, 0.4f, Color.Red, false);

        var direction = mousePos.Position - playerPos;
        var normalizedDirection = Vector2.Normalize(direction);
        var ray = new CollisionRay(playerPos, normalizedDirection, collisionMask: (int)CollisionGroup.MobLayer);
        var rayCastResults = _physics.IntersectRay(mousePos.MapId, ray, direction.Length(), playerEntity, false);

        foreach (var result in rayCastResults)
        {
            var ent = result.HitEntity;
            if (!_entity.HasComponent<MobStateComponent>(ent))
                continue;

            args.WorldHandle.DrawCircle(result.HitPos, 0.15f, Color.MediumVioletRed);
        }
    }
}

