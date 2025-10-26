using System.Numerics;
using Content.Shared._Starlight.Combat.Ranged.Pierce;
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

    [DataField]
    public EntProtoId BloodLineProto = "BloodLine";

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
    /// Шанс, что удар создаст линию крови.
    /// </summary>
    [DataField]
    public float BloodLineProbability = 0.1f;

    /// <summary>
    /// Количество крови, которое каждая капля заберет из тела персонажа.
    /// </summary>
    [DataField]
    public Vector2 BloodToTakePerParticle = new (0.5f, 3f);

    /// <summary>
    /// Количество крови, которое каждая линия из тела персонажа.
    /// </summary>
    [DataField]
    public Vector2 BloodToTakeToPerLine = new (1f, 3f);

    /// <summary>
    /// Уровень пробития брони для срабатывания эффекта.
    /// </summary>
    [DataField]
    public PierceLevel PierceLevel = PierceLevel.Wood;

    /// <summary>
    /// Угол разброса партиклов крови в градусах (например, 200 означает разброс в 200 градусов).
    /// </summary>
    [DataField]
    public float SpreadAngle = 250f;

    /// <summary>
    /// Звук, проигрывающийся при успешном спавне частичек крови
    /// </summary>
    [DataField]
    public SoundSpecifier ParticleSpawnedSound = new SoundCollectionSpecifier("BleedingStart", AudioParams.Default);

    /// <summary>
    /// Звук, проигрывающийся при успешном спавне линии крови
    /// </summary>
    [DataField]
    public SoundSpecifier BloodLineSpawnedSound = new SoundCollectionSpecifier("BleedingStart");
}
