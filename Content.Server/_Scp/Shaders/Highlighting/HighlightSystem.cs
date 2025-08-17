using Content.Shared._Scp.Shaders.Highlighting;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Scp.Shaders.Highlighting;

public sealed class HighlightSystem : SharedHighlightSystem
{
    /// <summary>
    /// <inheritdoc cref="SharedHighlightSystem.Highlight"/>
    /// </summary>
    public void NetHighlight(EntityUid target, EntityUid? recipient = null, int highlightTimes = 3)
    {
        var comp = EnsureComp<HighlightedComponent>(target);

        if (recipient.HasValue)
        {
            comp.Recipient = recipient;
            Dirty(target, comp);
        }

        var entity = GetNetEntity(target);

        var ev = new HighLightStartEvent(entity);
        RaiseNetworkEvent(ev);

        if (highlightTimes == -1)
            return;

        var time = OneHighlightTime * highlightTimes;

        Timer.Spawn(time,
            () =>
            {
                if (!Exists(target))
                    return;

                var endEvent = new HighLightEndEvent(entity);
                RaiseNetworkEvent(endEvent);

                RemCompDeferred<HighlightedComponent>(target);
            },
            Token.Token);
    }

    /// <summary>
    /// <inheritdoc cref="SharedHighlightSystem.HighLightAll"/>
    /// </summary>
    public void NetHighlightAll(IEnumerable<EntityUid> list, EntityUid? recipient = null)
    {
        foreach (var uid in list)
        {
            NetHighlight(uid, recipient);
        }
    }

    /// <summary>
    /// <inheritdoc cref="SharedHighlightSystem.HighLightAll"/>
    /// </summary>
    public void NetHighlightAll(ReadOnlySpan<EntityUid> list, EntityUid? recipient = null)
    {
        foreach (var uid in list)
        {
            NetHighlight(uid, recipient);
        }
    }
}
