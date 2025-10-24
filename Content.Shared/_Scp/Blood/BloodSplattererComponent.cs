using System.Numerics;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Blood;

/// <summary>
/// Компонент, отвечающий за создание партиклов крови при ударе.
/// Выдается атакующему.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BloodSplattererComponent : Component
{
    /// <summary>
    /// Прототипы партиклов, которые будут разлетаться от удара по сущности.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> Particles =
    [
        "BloodParticle1",
        "BloodParticle2",
        "BloodParticle3",
    ];

    /// <summary>
    /// Количество создаваемых частиц крови (min, max).
    /// </summary>
    [DataField]
    public Vector2i Amount = new(1, 2);

    /// <summary>
    /// Расстояние, которое пролетит частица крови до падения.
    /// </summary>
    [DataField]
    public Vector2 Distance = new(5f, 20f);

    /// <summary>
    /// Шанс, что удар вызовет разбрызгивание крови.
    /// </summary>
    [DataField]
    public float Probability = 0.5f;

    /// <summary>
    /// Количество крови, которое каждая капля заберет из тела персонажа.
    /// TODO: Рандомизировать.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodToTake = FixedPoint2.New(5);

    /// <summary>
    /// Уровень пробития брони для срабатывания эффекта.
    /// </summary>
    [DataField]
    public PierceLevel PierceLevel = PierceLevel.HardenedMetal;

    /// <summary>
    /// Угол разброса партиклов крови в градусах (например, 200 означает разброс в 200 градусов).
    /// </summary>
    [DataField]
    public float SpreadAngle = 250f;

    /// <summary>
    /// Звук, проигрывающийся при успешной атаке.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;
}
