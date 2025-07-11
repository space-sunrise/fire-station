using System.Threading;
using Content.Shared.GameTicking;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Shared._Scp.Shaders.Highlighting;

/// <summary>
/// Система-помощник для подсвечивания сущностей через шейдер подсвечивания
/// </summary>
public abstract class SharedHighlightSystem : EntitySystem
{
    /// <summary>
    /// Примерное время одного подсвечивания.
    /// </summary>
    public static readonly TimeSpan OneHighlightTime = TimeSpan.FromSeconds(1.6f);

    private static CancellationTokenSource _token = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(_ => RecreateToken());
    }

    /// <summary>
    /// Подсвечивает сущность указанное количество раз.
    /// Чтобы подсвечивать бесконечно передать -1.
    /// </summary>
    /// <param name="target">Сущность, которая будет подсвечена</param>
    /// <param name="highlightTimes">Количество раз подсвечивания</param>
    public void Highlight(EntityUid target, int highlightTimes = 3)
    {
        AddComp<HighlightedComponent>(target);

        if (highlightTimes == -1)
            return;

        var time = OneHighlightTime * highlightTimes;

        Timer.Spawn(time, () => RemComp<HighlightedComponent>(target), _token.Token);
    }

    private static void RecreateToken()
    {
        _token.Cancel();
        _token = new();
    }
}
