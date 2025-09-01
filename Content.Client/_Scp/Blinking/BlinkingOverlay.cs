using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Blinking;

public sealed class BlinkingOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    private bool _isAnimating;

    private float _blinkingProgress;
    private float _targetProgress;
    private float _animationDuration = 5f; // 0.5 секунды, чтобы не было слишком долго

    private float _timer;

    public BlinkingOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = 999;
        _shader = _prototype.Index<ShaderPrototype>("BlinkingEffect").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!_isAnimating)
            return;

        _timer += args.DeltaSeconds;
        var t = Math.Clamp(_timer / _animationDuration, 0f, 1f);

        // Линейная интерполяция между стартовым и целевым значением
        _blinkingProgress = MathHelper.Lerp(_blinkingProgress, _targetProgress, t);

        if (t >= 1f)
        {
            _timer = 0f;
            _isAnimating = false;
        }
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        if (!_player.LocalEntity.HasValue)
            return;

        if (MathHelper.CloseTo(_blinkingProgress, 0f))
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("blinkProgress", _blinkingProgress);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);
        worldHandle.UseShader(null);
    }

    public void OpenEyes()
    {
        _targetProgress = 0f;
        _timer = 0f;
        _isAnimating = true;
    }

    public void CloseEyes()
    {
        _targetProgress = 1f;
        _timer = 0f;
        _isAnimating = true;
    }

    public bool AreEyesClosed()
    {
        return MathHelper.CloseTo(_blinkingProgress, 1f);
    }
}
