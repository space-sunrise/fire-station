using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Настройки ритуала SCP-933.
/// Все параметры ритуала в одном компоненте.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933RitualSettingsComponent : Component
{
    /// <summary>
    /// Разрешить оживление при переходе в состояние хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowRevives = true;

    /// <summary>
    /// Исцелять хоста при появлении.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HealHostOnEmerge = true;

    /// <summary>
    /// Исцелять жертв при появлении хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HealVictimsOnHostEmerge = true;

    /// <summary>
    /// Количество исцеления (фиксированное или процент от максимального).
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 HealAmount = FixedPoint2.Zero; // Zero = полное исцеление

    /// <summary>
    /// Пороги здоровья для хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MobHealthThresholds HostHealthThresholds = new();

    /// <summary>
    /// Урон ближнего боя для хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HostMeleeSettings MeleeSettings = new();

    /// <summary>
    /// Может ли хост иметь несколько жертв.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowMultipleVictims = true;

    /// <summary>
    /// Максимальное количество жертв.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxVictims = 10;

    /// <summary>
    /// Может ли жертва стать хостом после смерти текущего.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool VictimsCanInheritHost = false;

    /// <summary>
    /// Требуется ли лента на лице для поддержания статуса хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool RequireTapeForHost = false;

    /// <summary>
    /// Что происходит при смерти хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HostDeathBehavior OnHostDeath = HostDeathBehavior.TransferToVictim;
}

[DataDefinition, Serializable]
public sealed partial class MobHealthThresholds
{
    /// <summary>
    /// Порог живого состояния.
    /// </summary>
    [DataField]
    public FixedPoint2 Alive = FixedPoint2.Zero;

    /// <summary>
    /// Порог критического состояния.
    /// </summary>
    [DataField]
    public FixedPoint2 Critical = 700;

    /// <summary>
    /// Порог смерти.
    /// </summary>
    [DataField]
    public FixedPoint2 Dead = 800;
}

[DataDefinition, Serializable]
public sealed partial class HostMeleeSettings
{
    /// <summary>
    /// Тип урона.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> DamageType = "Blunt";

    /// <summary>
    /// Количество урона.
    /// </summary>
    [DataField]
    public FixedPoint2 DamageAmount = 25;

    /// <summary>
    /// Дальность атаки.
    /// </summary>
    [DataField]
    public float Range = 1.5f;

    /// <summary>
    /// Угол атаки.
    /// </summary>
    [DataField]
    public float Angle = 60;
}

public enum HostDeathBehavior : byte
{
    /// <summary>
    /// Все жертвы умирают.
    /// </summary>
    AllDie,

    /// <summary>
    /// Передать хоста первой жертве.
    /// </summary>
    TransferToVictim,

    /// <summary>
    /// Освободить всех жертв.
    /// </summary>
    ReleaseVictims,

    /// <summary>
    /// Ничего не происходит.
    /// </summary>
    Nothing,
}
