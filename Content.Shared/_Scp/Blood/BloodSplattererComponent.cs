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
    /// Прототипы партиклов для спавна
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> Particles =
    [
        "BloodSplatter1",
        "BloodSplatter2",
        "BloodSplatter3",
        "BloodSplatter4",
        "BloodSplatter5",
        "BloodSplatter6",
    ];

    /// <summary>
    /// Количество создаваемых частиц крови (min, max).
    /// </summary>
    [DataField]
    public Vector2i Amount = new(1, 2);

    /// <summary>
    /// Скорость полета частиц (min, max).
    /// </summary>
    [DataField]
    public Vector2 Speed = new(3f, 40f);

    /// <summary>
    /// Общее количество крови, забираемое у жертвы для всех частиц.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodToTake = FixedPoint2.New(10);

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
