using Content.Shared.Access.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared.Access;

/// <summary>
///     Contains a list of access tags that are part of this group.
///     Used by <see cref="AccessComponent"/> to avoid boilerplate.
/// </summary>
[Prototype]
public sealed partial class AccessGroupPrototype : IPrototype
{
    [NonSerialized]
    private static IPrototypeManager? _prototype;

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The player-visible name of the access level group
    /// </summary>
    [DataField]
    public string? Name { get; set; }

    /// <summary>
    /// The access levels associated with this group
    /// </summary>
    // Fire edit start
    public HashSet<ProtoId<AccessLevelPrototype>> Tags
    {
        get => GetAllTags();
        set => _tags = value;
    }

    [DataField("tags")]
    private HashSet<ProtoId<AccessLevelPrototype>> _tags = [];

    [DataField]
    public HashSet<ProtoId<AccessGroupPrototype>> Groups = [];
    // Fire edit end

    public string GetAccessGroupName()
    {
        if (Name is { } name)
            return Loc.GetString(name);

        return ID;
    }

    // Fire added start - для улучшения читабельности прототипов
    private HashSet<ProtoId<AccessLevelPrototype>> GetAllTags()
    {
        _prototype ??= IoCManager.Resolve<IPrototypeManager>();

        var result = new HashSet<ProtoId<AccessLevelPrototype>>();
        var visitedGroups = new HashSet<string>(); // для защиты от циклов

        CollectTagsRecursive(this, result, visitedGroups, ref _prototype);

        return result;
    }

    private static void CollectTagsRecursive(
        AccessGroupPrototype group,
        HashSet<ProtoId<AccessLevelPrototype>> result,
        HashSet<string> visitedGroups,
        ref IPrototypeManager prototype)
    {
        // Если уже посещали выходим
        if (!visitedGroups.Add(group.ID))
            return;

        // Добавляем свои теги
        foreach (var tag in group._tags)
        {
            result.Add(tag);
        }

        // Проходим по вложенным группам
        foreach (var protoId in group.Groups)
        {
            if (!prototype.TryIndex(protoId, out var subGroup))
                continue;

            CollectTagsRecursive(subGroup, result, visitedGroups, ref prototype);
        }
    }
    // Fire added end
}
