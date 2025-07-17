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
    /// <param name="recipient">Сущность, которая увидит подсвечивание. Если null, то его увидят все</param>
    /// <param name="highlightTimes">Количество раз подсвечивания</param>
    public void Highlight(EntityUid target, EntityUid? recipient = null, int highlightTimes = 3)
    {
        var comp = EnsureComp<HighlightedComponent>(target);

        if (recipient.HasValue)
        {
            comp.Recipient = recipient;
            Dirty(target, comp);
        }

        var ev = new HighLightStartEvent();
        RaiseLocalEvent(target, ref ev);

        if (highlightTimes == -1)
            return;

        var time = OneHighlightTime * highlightTimes;

        Timer.Spawn(time,
            () =>
            {
                if (!Exists(target))
                    return;

                var endEvent = new HighLightEndEvent();
                RaiseLocalEvent(target, ref endEvent);

                RemCompDeferred<HighlightedComponent>(target);
            },
            _token.Token);
    }

    /// <summary>
    /// Подсвечивает все сущности из списка
    /// </summary>
    public void HighLightAll(IEnumerable<EntityUid> list, EntityUid? recipient = null)
    {
        foreach (var uid in list)
        {
            Highlight(uid, recipient);
        }
    }

    private static void RecreateToken()
    {
        _token.Cancel();
        _token = new();
    }
}

[ByRefEvent]
public record struct HighLightStartEvent;

[ByRefEvent]
public record struct HighLightEndEvent;
