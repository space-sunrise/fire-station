using Content.Client._ES.Viewcone.Overlays;
using Content.Shared._Scp.Watching.FOV;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._ES.Viewcone;

/// <summary>
///     Handles adding and removing the viewcone overlays, as well as ferrying data between them
/// </summary>
public sealed class ESViewconeOverlayManagementSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    private ESViewconeConeOverlay _coneOverlay = default!;
    private ESViewconeSetAlphaOverlay _setAlphaOverlay = default!;
    private ESViewconeResetAlphaOverlay _resetAlphaOverlay = default!;

    // slightly balls state management, but
    // done so we don't have to requery within the same frame
    // this is always cleared at the end of resetting alpha
    // it is the least thread safe code of all time obviously. but rendering not threaded. so
    // we can abuse the fact that the overlays will always draw sequentially in the order we expect, and
    // one wont start rendering in the middle of rendering another
    [Access(typeof(ESViewconeSetAlphaOverlay), typeof(ESViewconeResetAlphaOverlay))]
    public List<(Entity<SpriteComponent> ent, float baseAlpha)> CachedBaseAlphas = new(128);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FieldOfViewComponent, ComponentInit>(OnConeManInit);
        SubscribeLocalEvent<FieldOfViewComponent, ComponentShutdown>(OnConeManShutdown);

        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<FieldOfViewComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        _coneOverlay = new();
        _setAlphaOverlay = new();
        _resetAlphaOverlay = new();
    }

    private void OnPlayerAttached(Entity<FieldOfViewComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        AddOverlays();
    }

    private void OnPlayerDetached(Entity<FieldOfViewComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        RemoveOverlays();
    }

    private void OnConeManInit(Entity<FieldOfViewComponent> ent, ref ComponentInit args)
    {
        if (_playerManager.LocalEntity == ent)
            AddOverlays();
    }

    private void OnConeManShutdown(Entity<FieldOfViewComponent> ent, ref ComponentShutdown args)
    {
        if (_playerManager.LocalEntity == ent)
            RemoveOverlays();
    }

    private void AddOverlays()
    {
        _overlayMan.AddOverlay(_coneOverlay);
        _overlayMan.AddOverlay(_setAlphaOverlay);
        _overlayMan.AddOverlay(_resetAlphaOverlay);
    }

    private void RemoveOverlays()
    {
        _overlayMan.RemoveOverlay(_coneOverlay);
        _overlayMan.RemoveOverlay(_setAlphaOverlay);
        _overlayMan.RemoveOverlay(_resetAlphaOverlay);
    }
}
