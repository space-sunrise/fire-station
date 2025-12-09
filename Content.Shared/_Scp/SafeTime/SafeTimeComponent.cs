using Robust.Shared.GameStates;

namespace Content.Shared._Scp.SafeTime;

/// <summary>
/// Компонент, отвечающий за наличие у сущности "безопасного времени".
/// В этом периоде она ограничена в своих способностях.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class SafeTimeComponent : Component
{
    /// <summary>
    /// Безопасное время, в течении которого работают некоторые ограничения, вроде запрета использования некоторых способностей.
    /// Сделано, чтобы дать игрокам фору в начале раунда на раскачку и стартовые подготовления(построения, брифинги)
    /// </summary>
    [DataField]
    public TimeSpan Time = TimeSpan.FromMinutes(15f);

    /// <summary>
    /// Время окончания безопасного времени <see cref="Time"/>
    /// </summary>
    [ViewVariables, AutoNetworkedField, AutoPausedField]
    public TimeSpan? TimeEnd;
}
