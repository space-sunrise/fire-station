using Content.Shared._Scp.Blinking;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Blinking;

public sealed class BlinkingOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override bool RequestScreenTexture => true;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _shader;

    public float BlinkProgress;

    public BlinkingOverlay()
    {
        IoCManager.InjectDependencies(this);
        _shader = _prototypeManager.Index<ShaderPrototype>("BlinkingEffect").InstanceUnique();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        var playerEntity = _playerManager.LocalEntity;
        if (playerEntity == null || !_entityManager.TryGetComponent<BlinkableComponent>(playerEntity, out var blinkable))
            return;

        var curTime = _timing.CurTime;
        BlinkProgress = curTime < blinkable.BlinkEndTime
            ? 1.0f
            : 0.0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null || BlinkProgress <= 0)
            return;

        if (_playerManager.LocalSession?.AttachedEntity is not { Valid: true })
            return;

        var worldHandle = args.WorldHandle;
        var viewport = args.WorldBounds;

        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("blinkProgress", BlinkProgress);

        worldHandle.UseShader(_shader);
        worldHandle.DrawRect(viewport, Color.White);

        worldHandle.UseShader(null);
    }
}
