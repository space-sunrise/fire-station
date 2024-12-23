using Content.Client.Actions;
using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._Scp.Scp173;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Movement.Events;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;

namespace Content.Client._Scp.Scp173;

// TODO: Здесь находится нереальный костыль, который является нереальной уязвимостью

public sealed class Scp173System : SharedScp173System
{
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ActionsSystem _actionsSystem = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private Scp173Overlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, MapInitEvent>(OnInit);
        SubscribeLocalEvent<Scp173Component, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<Scp173Component, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<Scp173Component, LocalPlayerDetachedEvent>(OnPlayerDetached);

        // TODO: Придумать исправить миспредикт способ лучше
        #region Movement

        SubscribeLocalEvent<Scp173Component, ChangeDirectionAttemptEvent>(OnDirectionAttempt);
        SubscribeLocalEvent<Scp173Component, UpdateCanMoveEvent>(OnMoveAttempt);

        #endregion

        _overlay = new(_transform, _ui.GetUIController<ActionUIController>(), _actionsSystem, _physics, _examine);
    }

    private void OnInit(EntityUid uid, Scp173Component component, MapInitEvent args)
    {
        if (_player.LocalSession?.AttachedEntity == uid)
            _overlayMan.AddOverlay(_overlay);
    }

    private void OnShutdown(EntityUid uid, Scp173Component component, ComponentShutdown args)
    {
        if (_player.LocalSession?.AttachedEntity == uid)
        {
            _overlayMan.RemoveOverlay(_overlay);
        }
    }

    private void OnPlayerAttached(EntityUid uid, Scp173Component component, LocalPlayerAttachedEvent args)
    {
        _overlayMan.AddOverlay(_overlay);
    }

    private void OnPlayerDetached(EntityUid uid, Scp173Component component, LocalPlayerDetachedEvent args)
    {
        _overlayMan.RemoveOverlay(_overlay);
    }

    // TODO: Придумать исправить миспредикт способ лучше
    #region Movement

    private void OnDirectionAttempt(Entity<Scp173Component> ent, ref ChangeDirectionAttemptEvent args)
    {
        // Разрешено, когда на нас не смотрят
        var allowed = !Is173Watched(ent, out _);
        ClientChangeMovement(allowed, args);
    }

    private void OnMoveAttempt(Entity<Scp173Component> ent, ref UpdateCanMoveEvent args)
    {
        // Разрешено, когда на нас не смотрят
        var allowed = !Is173Watched(ent, out _);
        ClientChangeMovement(allowed, args);
    }

    private void ClientChangeMovement(bool allowed, CancellableEntityEventArgs args)
    {
        // Если движение не разрешено отменяем ивент связанный с движением
        if (!allowed)
            args.Cancel();

        RaiseNetworkEvent(new ClientMovementChangedEvent(allowed));
    }

    #endregion
}
