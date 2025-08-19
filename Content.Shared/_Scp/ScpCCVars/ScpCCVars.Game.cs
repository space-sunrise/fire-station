using Robust.Shared.Configuration;

namespace Content.Shared._Scp.ScpCCVars;

public sealed partial class ScpCCVars
{
    /**
     * Auto open character window
     */

    /// <summary>
    /// Будет ли автоматически открываться меню персонажа, если игрок первый раз на должности?
    /// Устанавливается на клиенте игроком
    /// </summary>
    public static readonly CVarDef<bool> AutoOpenCharacterMenuClientSideEnabled =
        CVarDef.Create("scp.auto_open_character_menu", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Включено ли автоматическое открытие меню персонажа при заходе на должность впервые?
    /// Устанавливается на сервере, определяет ли вообще это включено.
    /// Игроки могут отключить у себя это в настройках.
    /// </summary>
    public static readonly CVarDef<bool> AutoOpenCharacterMenuServerSideEnabled =
        CVarDef.Create("scp.server_auto_open_character_menu", true, CVar.SERVERONLY | CVar.ARCHIVE);
}
