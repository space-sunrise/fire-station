﻿using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Blinking;

/// <summary>
/// Оверлей, отвечающий за графику и анимации моргания персонажа.
/// </summary>
public sealed class BlinkingOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;
    private static readonly ProtoId<ShaderPrototype> ShaderProtoId = "BlinkingEffect";

    public bool IsAnimating { get; private set; }

    private float _blinkingProgress;
    private float _targetProgress;
    private float _startProgress;

    /// <summary>
    /// Длина анимации моргания.
    /// Влияет как на открытие, так и на закрытие глаз.
    /// </summary>
    public float AnimationDuration = 0.4f;

    private float _timer;

    /// <summary>
    /// Экшен, вызываемый по окончании анимации.
    /// </summary>
    public event Action? OnAnimationFinished;

    public BlinkingOverlay()
    {
        IoCManager.InjectDependencies(this);

        ZIndex = 999;
        _shader = _prototype.Index(ShaderProtoId).InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (!IsAnimating)
            return;

        _timer += args.DeltaSeconds;
        var t = Math.Clamp(_timer / AnimationDuration, 0f, 1f);
        _blinkingProgress = MathHelper.Lerp(_startProgress, _targetProgress, t);

        if (t >= 1f)
        {
            _blinkingProgress = _targetProgress;
            _timer = 0f;
            IsAnimating = false;

            OnAnimationFinished?.Invoke();
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

    /// <summary>
    /// Открывает глаза в оверлее.
    /// </summary>
    public void OpenEyes()
    {
        _startProgress = _blinkingProgress;
        _targetProgress = 0f;
        _timer = 0f;
        IsAnimating = true;
    }

    /// <summary>
    /// Закрывает глаза в оверлее.
    /// </summary>
    public void CloseEyes()
    {
        _startProgress = _blinkingProgress;
        _targetProgress = 1f;
        _timer = 0f;
        IsAnimating = true;
    }

    /// <summary>
    /// Проверяет, закрыты ли глаза персонажа в оверлее.
    /// </summary>
    public bool AreEyesClosed()
    {
        return MathHelper.CloseTo(_blinkingProgress, 1f);
    }
}
