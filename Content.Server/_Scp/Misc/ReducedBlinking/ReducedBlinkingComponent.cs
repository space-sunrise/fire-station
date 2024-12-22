using Robust.Shared.Audio;

namespace Content.Server._Scp.Misc.ReducedBlinking;

[RegisterComponent]
public sealed partial class ReducedBlinkingComponent : Component
{
    /// <summary>
    /// Сколько времени будет добавляться к следующему времени моргания
    /// </summary>
    [DataField(required:true), ViewVariables(VVAccess.ReadWrite)]
    public float BonusTime;

    /// <summary>
    /// Время применения(дуафтера) предмета
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ApplicationTime = 2f;

    /// <summary>
    /// Количество использований предмета
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int UsageCount = 3;

    [DataField, ViewVariables]
    public SoundSpecifier? UseSound;
}
