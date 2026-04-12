namespace Content.Shared._Scp.Proximity;

/// <summary>
/// Runtime-компонент, который существует только пока цель находится рядом
/// хотя бы с одним <see cref="ProximityReceiverComponent"/>.
/// Хранит выбранный dominant receiver для дальнейшей логики.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveProximityTargetComponent : Component
{
    /// <summary>
    /// Текущий dominant receiver, рядом с которым находится цель.
    /// </summary>
    [ViewVariables]
    public EntityUid Receiver;

    /// <summary>
    /// Эффективный радиус receiver, внутри которого находится цель.
    /// </summary>
    [ViewVariables]
    public float CloseRange;
}
