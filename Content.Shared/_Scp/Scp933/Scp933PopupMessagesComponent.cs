using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Сообщения (popups) для SCP-933.
/// Все строки локализации вынесены для кастомизации.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933PopupMessagesComponent : Component
{
    /// <summary>
    /// Сообщение при начале отрыва.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PeelStartMessage = "scp933-peel-start";

    /// <summary>
    /// Сообщение при успешном отрыве.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PeelSuccessMessage = "scp933-peel-success";

    /// <summary>
    /// Сообщение при провале отрыва (нет места в руках).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string PeelHandFailMessage = "scp933-peel-hand-fail";

    /// <summary>
    /// Сообщение при начале наклейки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ApplyStartMessage = "scp933-apply-start";

    /// <summary>
    /// Сообщение при успешной наклейке (пользователь).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ApplySuccessUserMessage = "scp933-apply-success-user";

    /// <summary>
    /// Сообщение при успешной наклейке (цель).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ApplySuccessTargetMessage = "scp933-apply-success-target";

    /// <summary>
    /// Сообщение при провале наклейки.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ApplyFailMessage = "scp933-tape-equip-fail";

    /// <summary>
    /// Сообщение при попытке наклеить повторно.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string AlreadyHasTapeMessage = "scp933-tape-already";

    /// <summary>
    /// Сообщение при начале срыва.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string RipStartMessage = "scp933-rip-start";

    /// <summary>
    /// Сообщение при успешном срыве (пользователь).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string RipSuccessUserMessage = "scp933-rip-success-user";

    /// <summary>
    /// Сообщение при успешном срыве (цель).
    /// </summary>
    [DataField, AutoNetworkedField]
    public string RipSuccessTargetMessage = "scp933-rip-success-target";

    /// <summary>
    /// Сообщение при срыве лица жертвы.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string FaceTornMessage = "scp933-victim-face-torn";

    /// <summary>
    /// Сообщение при появлении хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string HostEmergedMessage = "scp933-host-emerged";

    /// <summary>
    /// Сообщение "только мастер может сорвать".
    /// </summary>
    [DataField, AutoNetworkedField]
    public string RipMasterOnlyMessage = "scp933-rip-master-only";
}
