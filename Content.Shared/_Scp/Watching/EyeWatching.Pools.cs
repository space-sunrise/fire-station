using Content.Shared._Scp.Blinking;

namespace Content.Shared._Scp.Watching;

public sealed partial class EyeWatchingSystem
{
    private readonly Stack<List<EntityUid>> _uidListPool = new();
    private readonly Stack<List<Entity<BlinkableComponent>>> _blinkableListPool = new();
    private readonly Stack<HashSet<Entity<BlinkableComponent>>> _blinkableSetPool = new();

    #region Rent

    private List<EntityUid> RentUidList()
    {
        return _uidListPool.TryPop(out var list)
            ? list
            : [];
    }

    private List<Entity<BlinkableComponent>> RentBlinkableList()
    {
        return _blinkableListPool.TryPop(out var list)
            ? list
            : [];
    }

    private HashSet<Entity<BlinkableComponent>> RentBlinkableSet()
    {
        return _blinkableSetPool.TryPop(out var set)
            ? set
            : [];
    }

    #endregion

    #region Return

    private void ReturnUidList(List<EntityUid> list)
    {
        list.Clear();
        _uidListPool.Push(list);
    }

    private void ReturnBlinkableList(List<Entity<BlinkableComponent>> list)
    {
        list.Clear();
        _blinkableListPool.Push(list);
    }

    private void ReturnBlinkableSet(HashSet<Entity<BlinkableComponent>> set)
    {
        set.Clear();
        _blinkableSetPool.Push(set);
    }

    #endregion
}
