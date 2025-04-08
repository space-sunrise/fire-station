using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Shaders.Warping;

public sealed class WarpOverlay : Overlay
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override bool RequestScreenTexture => true;
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    private readonly ShaderInstance _warpShader;

    private readonly TimeSpan _startTime;

    private const float StartIntensity = 0.5f;
    private const float FinalIntensity = 10.0f;
    private const float IntensityIncrement = 0.1f;
    private const double TimeInterval = 6.0d;

    public WarpOverlay(TimeSpan startedTime)
    {
        IoCManager.InjectDependencies(this);
        _warpShader = _prototypeManager.Index<ShaderPrototype>("Warping").InstanceUnique();

        _startTime = startedTime;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.WorldHandle;
        var viewport = args.WorldBounds;

        // Вычисление прошедшего времени
        var elapsedTime = _timing.CurTime - _startTime;

        // Вычисление интенсивности эффекта
        var intensity = StartIntensity + (float)(elapsedTime.TotalSeconds / TimeInterval) * IntensityIncrement;
        intensity = Math.Clamp(intensity, StartIntensity, FinalIntensity);

        // Применяем интенсивность в шейдер
        _warpShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _warpShader.SetParameter("warp_intensity", intensity);
        _warpShader.SetParameter("time_scale", 5.0f);

        handle.UseShader(_warpShader);
        handle.DrawRect(viewport, Color.White);
        handle.UseShader(null);
    }
}
