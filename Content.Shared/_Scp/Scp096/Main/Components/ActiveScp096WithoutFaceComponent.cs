using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

// TODO: Звуки(нормальные)
// TODO: Возможность визуально понять степень урона лица скромника со стороны(на спрайте, визуально)
/// <summary>
/// Компонент, отвечающий за активное состояние скромника с содранным лицом.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class ActiveScp096WithoutFaceComponent : Component
{
    /// <summary>
    /// Звук перехода в состояние содранного лица
    /// </summary>
    [DataField]
    public SoundSpecifier? StartSound;

    /// <summary>
    /// Звук возвращения в стандартное состояние
    /// </summary>
    [DataField]
    public SoundSpecifier? ShutdownSound;

    /// <summary>
    /// Звук, который будет издавать скромник в состоянии.
    /// </summary>
    [DataField]
    public SoundSpecifier AmbientSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/withoutface.ogg",
        AudioParams.Default.WithMaxDistance(15f).WithVolume(7f).WithRolloffFactor(5f).WithLoop(true));

    /// <summary>
    /// Скорость передвижения в данном состоянии
    /// </summary>
    [DataField]
    public float Speed = 3.5f;

    /// <summary>
    /// Теги, которые будут выданы при переходе в состояние
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> TagsToAdd = new()
    {
        "DoorBumpOpener",
    };

    #region Melee attack

    /// <summary>
    /// Урон, который будет у скромника в данном состоянии
    /// </summary>
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict = new()
        {
            { "Slash", 15 },
            { "Blunt", 1 },
            { "Structural", 45 },
        },
    };

    /// <summary>
    /// Скорость атаки в данном состоянии
    /// </summary>
    [DataField]
    public float AttackRate = 0.2f;

    /// <summary>
    /// Модификатор урона по стамине в зависимости от Blunt урона в данном состоянии.
    /// Каждая единица Blunt урона, нанесенного скромником в данном состоянии, будет выдавать данное количество урона по стамине.
    /// </summary>
    [DataField]
    public FixedPoint2 StaminaDamageFactor = 100f;

    /// <summary>
    /// Сохраненный предыдущий урон скромника.
    /// Будет возвращен после выхода из состояния
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public DamageSpecifier? CachedDamage;

    /// <summary>
    /// Сохраненная скорость атаки скромника.
    /// Будет возвращена после выхода из состояния
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float? CachedAttackRate;

    /// <summary>
    /// Сохраненный предыдущий урон по стамине скромника.
    /// Будет возвращен после выхода из состояния
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2? CachedStaminaDamageFactor;

    #endregion

    #region Throw

    /// <summary>
    /// Дистанция отбрасывания, на которую будет отбрасывать скромник при ударе цели в данном состоянии
    /// </summary>
    [DataField]
    public float ThrowDistance = 1.5f;

    /// <summary>
    /// Скорость отбрасывания, которая будет использоваться при ударе цели в данном состоянии
    /// </summary>
    [DataField]
    public float ThrowSpeed = 10f;

    /// <summary>
    /// Сохраненная дистанция отбрасывания.
    /// Будет возвращена после выхода из состояния
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float? CachedThrowDistance;

    /// <summary>
    /// Сохраненная скорость отбрасывания.
    /// Будет возвращена при выходе из состояния
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float? CachedThrowSpeed;

    #endregion
}
