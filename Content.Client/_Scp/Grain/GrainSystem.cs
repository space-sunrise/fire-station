using Robust.Client.Graphics;
using Robust.Shared.Player;
using Robust.Shared.Configuration;
using Content.Shared._Scp.ScpCCVars;

namespace Content.Client._Scp.Grain;

// TODO: Коммон оверлей систем
public sealed class GrainOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private GrainOverlay _overlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _overlay = new ();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        _cfg.OnValueChanged(ScpCCVars.GrainToggleOverlay, OnGrainToggleOverlayOptionChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _cfg.UnsubValueChanged(ScpCCVars.GrainToggleOverlay, OnGrainToggleOverlayOptionChanged);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        AddOverlay();
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        RemoveOverlay();
    }

    private void OnGrainToggleOverlayOptionChanged(bool option)
    {
        if (option)
        {
            AddOverlay();
        }
        else
        {
            RemoveOverlay();
        }
    }

    #region Public API

    public void ToggleOverlay()
    {
        if (_cfg.GetCVar(ScpCCVars.GrainToggleOverlay))
        {
            if (_overlayManager.HasOverlay<GrainOverlay>())
                RemoveOverlay();
            else
                AddOverlay();
        }
    }

    public void AddOverlay()
    {
        if (_cfg.GetCVar(ScpCCVars.GrainToggleOverlay) && !_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.AddOverlay(_overlay);
    }

    public void RemoveOverlay()
    {
        if (_overlayManager.HasOverlay<GrainOverlay>())
            _overlayManager.RemoveOverlay(_overlay);
    }

    #endregion
}