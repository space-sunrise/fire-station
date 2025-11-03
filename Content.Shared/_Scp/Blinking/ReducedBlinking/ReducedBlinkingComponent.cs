using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Blinking.ReducedBlinking;

[RegisterComponent, NetworkedComponent]
public sealed partial class ReducedBlinkingComponent : Component
{
    /// <summary>
    /// Сколько времени будет добавляться к следующему времени моргания
    /// </summary>
    [DataField(required:true)]
    public TimeSpan FirstBlinkingBonusTime;

    [DataField(required:true)]
    public TimeSpan OtherBlinkingBonusTime;

    [DataField(required:true)]
    public TimeSpan OtherBlinkingBonusDuration;

    /// <summary>
    /// Время применения(дуафтера) предмета
    /// </summary>
    [DataField]
    public TimeSpan ApplicationTime = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Количество использований предмета
    /// </summary>
    [DataField]
    public int UsageCount = 3;

    [DataField]
    public SoundSpecifier? UseSound = new SoundCollectionSpecifier("EyeDropletsUse",
        AudioParams.Default.WithMaxDistance(3f).WithVariation(0.125f));
}
