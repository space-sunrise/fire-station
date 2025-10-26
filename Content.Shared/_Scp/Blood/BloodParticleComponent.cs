using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Blood;

/// <summary>
/// Компонент партикла крови, который разлетается от удара по персонажу.
/// Выдается самому партиклу
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodParticleComponent : Component
{
    /// <summary>
    /// Лужицы крови, которые будут оставаться на месте падения частички.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> BloodEntities =
    [
        "BloodSplatter1",
        "BloodSplatter2",
        "BloodSplatter3",
        "BloodSplatter4",
        "BloodSplatter5",
        "BloodSplatter6",
    ];

    /// <summary>
    /// Звук, проигрываемый при появлении частицы.
    /// </summary>
    [DataField]
    public SoundSpecifier LandSound = new SoundCollectionSpecifier("BloodDripping",
        AudioParams.Default.AddVolume(-7f).WithMaxDistance(3f).WithVariation(0.125f));

    /// <summary>
    /// Время полета частички крови.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FlyTime = TimeSpan.FromSeconds(1f);

    /// <summary>
    /// Процентный показатель рандомизации времени полета <see cref="FlyTime"/>.
    /// Прибавляется к времени полета как (0-25%) от текущего времени полета
    /// </summary>
    [DataField]
    public float FlyTimeVariation = 0.7f;

    /// <summary>
    /// Количество промежутков, между которыми частичка будет двигаться.
    /// </summary>
    [DataField]
    public int MoveTimes = 40;

    /// <summary>
    /// Ускорение частички и направление движения.
    /// Задается внутри кода исходя из расположения атакующего и цели относительно него.
    /// </summary>
    [ViewVariables]
    public Vector2? Velocity;

    /// <summary>
    /// Скорость движения частички.
    /// Задается внутри кода исходя из расстояния, которое нужно пройти и <see cref="MoveTimes"/>
    /// </summary>
    [ViewVariables]
    public Vector2 Speed = Vector2.Zero;

    /// <summary>
    /// Задается внутри кода исходя из <see cref="FlyTime"/> и <see cref="MoveTimes"/>
    /// </summary>
    [ViewVariables]
    public TimeSpan MoveCooldown;

    /// <summary>
    /// Время следующего движения частички.
    /// Задается исходя из текущего времени и <see cref="MoveCooldown"/>
    /// </summary>
    [ViewVariables]
    public TimeSpan NextMoveTime = TimeSpan.Zero;
}
