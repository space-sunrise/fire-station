using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

/// <summary>
/// Компонент, отвечающий за поведение скромника.
/// Служит ключевым для перехода в другие состояния.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class Scp096Component : Component
{
    /// <summary>
    /// Угол, под которым видно лицо скромника.
    /// Требуется, чтобы жертва и скромник смотрели друг на друга под данным углом одновременно.
    /// </summary>
    [DataField]
    public float ArgoAngle = 25f;

    /// <summary>
    /// Скорость передвижения скромника в обычном состоянии.
    /// </summary>
    [DataField]
    public float Speed = 1.5f;

    /// <summary>
    /// Какие предметы скромник сможет поднимать?
    /// </summary>
    [DataField]
    public EntityWhitelist? PickupWhitelist;

    /// <summary>
    /// Какие предметы скромник НЕ сможет поднимать?
    /// </summary>
    [DataField]
    public EntityWhitelist? PickupBlacklist;

    /// <summary>
    /// Количество целей скромника.
    /// </summary>
    /// <remarks>
    /// Кешируем здесь для быстрого подсчет в виджете.
    /// Сохраняется в базовом компоненте для удобства, так как цели могут быть как в состоянии ярости, так и пред-агр состоянии.
    /// </remarks>
    [ViewVariables, AutoNetworkedField]
    public uint TargetsCount;

    #region Sounds

    /// <summary>
    /// Звук плача, который будет издавать вокруг себя скромник в стандартном состоянии
    /// </summary>
    [DataField]
    public SoundSpecifier CrySound = new SoundPathSpecifier("/Audio/_Scp/Scp096/crying.ogg",
        AudioParams.Default.WithVolume(-14f).WithMaxDistance(4f).WithRolloffFactor(5f).WithLoop(true));

    /// <summary>
    /// Звук, проигрывающийся жертве, когда она видит скромника.
    /// </summary>
    [DataField]
    public SoundSpecifier SeenSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/seen.ogg");

    /// <summary>
    /// Сущность звука, который издается вокруг скромника.
    /// </summary>
    [ViewVariables, AutoNetworkedField, NonSerialized]
    public EntityUid? AudioStream;

    #endregion

    #region Animations

    /// <summary>
    /// Длина анимаций сидения и вставания
    /// </summary>
    [DataField]
    public TimeSpan AnimationDuration = TimeSpan.FromSeconds(2f);

    /// <summary>
    /// Действует ли анимация перехода от агрессивного состояния в мертвое?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool AgroToDeadAnimation;

    /// <summary>
    /// Действует ли анимация перехода от мертвого состояния в обычное?
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool DeadToIdleAnimation;

    #endregion

    #region CryOut

    /// <summary>
    /// Урон, который будет наносить способность плача скромника.
    /// </summary>
    [DataField]
    public DamageSpecifier CryOutDamage = new ()
    {
        DamageDict = new()
        {
            { "Structural", 50 },
        },
    };

    /// <summary>
    /// Радиус нанесения урона от способности плача
    /// </summary>
    [DataField]
    public float CryOutRange = 6f;

    /// <summary>
    /// Белый список сущностей, которые будут получать урон от способности плача.
    /// </summary>
    [DataField]
    public EntityWhitelist CryOutWhitelist = new ()
    {
        Tags = new List<ProtoId<TagPrototype>>
        {
            "Wall",
            "Window",
            "Directional",
        },
    };

    /// <summary>
    /// Звук, который будет произноситься во время активации способности плача
    /// </summary>
    [DataField]
    public SoundSpecifier CryOutSound = new SoundCollectionSpecifier("IdleScp096",
        AudioParams.Default.WithMaxDistance(6f));

    /// <summary>
    /// Длительность тряски скромника во время активации плача
    /// </summary>
    [DataField]
    public TimeSpan CryOutJitterTime = TimeSpan.FromSeconds(1f);

    #endregion

    #region Face skin rip

    /// <summary>
    /// Длительность полоски действия, которой необходимо завершиться активации действия способности сдирания кожи с лица
    /// </summary>
    [DataField]
    public TimeSpan FaceSkinRipDoAfterTime = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// Урон, который будет получать лицо скромника при активации способности сдирания кожи
    /// </summary>
    [DataField]
    public DamageSpecifier FaceSkinRipDamageToFace = new()
    {
        DamageDict = new()
        {
            { "Blunt", 20 },
        },
    };

    /// <summary>
    /// Звук, который будет проигрываться при активации способности сдирания кожи
    /// </summary>
    [DataField]
    public SoundSpecifier? FaceSkinRipDamageToFaceSound = new SoundCollectionSpecifier("FaceSkinRip");

    /// <summary>
    /// Прототип сущности лица, который будет заспавнен при инициализации скромника на карте
    /// </summary>
    [DataField]
    public EntProtoId FaceProto = "Scp096Face";

    /// <summary>
    /// Слот, в котором хранится лицо скромника.
    /// </summary>
    [DataField]
    public string FaceContainer = "face_slot";

    /// <summary>
    /// Ссылка на лицо скромника.
    /// Быстрый способ получить лицо, избежав работы с контейнерами.
    /// </summary>
    [ViewVariables, AutoNetworkedField, NonSerialized]
    public EntityUid? FaceEntity;

    #endregion
}
