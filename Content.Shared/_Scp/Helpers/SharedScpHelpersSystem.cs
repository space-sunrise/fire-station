using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared._Scp.Helpers;

public abstract class SharedScpHelpersSystem : EntitySystem
{
    #region Percente

    /// <summary>
    /// Возвращает список с данным процентным соотношением
    /// </summary>
    /// <param name="sourceList">Исходный список</param>
    /// <param name="percentage">Процент от 0 до 100</param>
    /// <typeparam name="T">Компонент</typeparam>
    [Obsolete]
    public IEnumerable<T> GetPercentageOfHashSet<T>(IReadOnlyCollection<T> sourceList, float percentage) where T : IComponent
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);
        return sourceList.Where(t => !Transform(t.Owner).Anchored).Take(countToAdd).ToHashSet();
    }

    /// <summary>
    /// Возвращает список с данным процентным соотношением
    /// </summary>
    /// <param name="sourceList">Исходный список</param>
    /// <param name="percentage">Процент от 0 до 100</param>
    /// <typeparam name="T">Ентити с компонентом</typeparam>
    public IEnumerable<Entity<T>> GetPercentageOfHashSet<T>(IReadOnlyCollection<Entity<T>> sourceList, float percentage) where T : IComponent
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);
        return sourceList.Where(e => !Transform(e).Anchored).Take(countToAdd).ToHashSet();
    }

    /// <summary>
    /// Возвращает список с данным процентным соотношением
    /// </summary>
    /// <param name="sourceList">Исходный список</param>
    /// <param name="percentage">Процент от 0 до 100</param>
    public IEnumerable<EntityUid> GetPercentageOfHashSet(IReadOnlyCollection<EntityUid> sourceList, float percentage)
    {
        var countToAdd = (int) Math.Round((double) sourceList.Count * percentage / 100);
        return sourceList.Where(e => !Transform(e).Anchored).Take(countToAdd).ToHashSet();
    }

    #endregion

    #region Get All/First entity

    /// <summary>
    /// Получает все список всех ентити с компонентом и возвращает.
    /// Удобно для использования, так как не требует засорять код лишним циклом
    /// </summary>
    /// <typeparam name="T">Компонент</typeparam>
    /// <remarks>Список может быть пустым, если ничего не найдено</remarks>
    /// <returns>Полный список всех ентити в игре с данным компонентом</returns>
    public IEnumerable<Entity<T>> GetAll<T>() where T : IComponent
    {
        var query = EntityManager.AllEntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var component))
        {
            yield return (uid, component);
        }
    }

    /// <summary>
    /// Возвращает первый попавшийся ентити с данным компонентом
    /// </summary>
    /// <param name="entity">Возвращаемый ентити</param>
    /// <typeparam name="T">Компонент</typeparam>
    /// <returns>Первый попавшийся ентити с данным компонентом</returns>
    public bool TryGetFirst<T>([NotNullWhen(true)] out Entity<T>? entity) where T : IComponent
    {
        entity = null;

        var query = EntityManager.AllEntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var component))
        {
            entity = (uid, component);
            return true;
        }

        return false;
    }

    #endregion
}
