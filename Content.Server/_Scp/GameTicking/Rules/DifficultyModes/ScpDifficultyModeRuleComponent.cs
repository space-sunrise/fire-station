using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Mobs.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.GameTicking.Rules.DifficultyModes;

[RegisterComponent]
public sealed partial class ScpDifficultyModeRuleComponent : Component
{
    /// <summary>
    /// Какой параметр мин-макс будет означать неограниченные слоты
    /// </summary>
    public static readonly MinMaxExtended UnlimitedSlotsFlag = new (-1, -1);

    /// <summary>
    /// Количество доступных в этом режиме игры SCP объектов выбранного класса.
    /// Работает только для игровых объектов, которые имеют работы.
    /// Ключ - класс содержания SCP объекта.
    /// Значение - количество в формате мин-макс. -1 будет означать неограниченное количество
    /// </summary>
    [DataField(required: true)]
    public Dictionary<Classification, MinMaxExtended> ScpSlots;

    #region Whitelist and blacklist

    /// <summary>
    /// Вайтлист объектов, которые будут доступны в раунде.
    /// Применяется к игровым SCP
    /// </summary>
    [DataField]
    public ComponentRegistry? PlayableWhitelist;

    /// <summary>
    /// Блеклист объектов, которые будут доступны в раунде.
    /// Применяется к игровым SCP
    /// </summary>
    [DataField]
    public ComponentRegistry? PlayableBlacklist;

    /// <summary>
    /// Вайтлист объектов, которые будут доступны в раунде.
    /// Применяется к НЕ игровым SCP(предметам)
    /// </summary>
    [DataField]
    public EntityWhitelist? ItemWhitelist;

    /// <summary>
    /// Блеклист объектов, которые будут доступны в раунде.
    /// Применяется к НЕ игровым SCP(предметам)
    /// </summary>
    [DataField]
    public EntityWhitelist? ItemBlacklist;

    #endregion
}
