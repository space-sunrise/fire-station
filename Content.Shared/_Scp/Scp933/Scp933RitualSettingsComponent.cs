using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Настройки ритуала SCP-933: хост, жертвы, визуал, бой.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp933RitualSettingsComponent : Component
{
    // === Host ===

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
    /// Пороги здоровья для хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public MobHealthThresholds HostHealthThresholds = new();

    /// <summary>
    /// Урон ближнего боя для хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public DamageSpecifier MeleeDamage = new() { DamageDict = { ["Blunt"] = 25 } };

    /// <summary>
    /// Дальность атаки ближнего боя для хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MeleeRange = 1.5f;

    /// <summary>
    /// Угол атаки ближнего боя для хоста.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MeleeAngle = 60;

    // === Visuals (migrated from Scp933VisualEffectsComponent) ===

    /// <summary>
    /// Скрывать ли глаза при наложении ленты.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HideEyes = true;

    /// <summary>
    /// Скрывать ли морду при наложении ленты.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HideSnout = true;

    /// <summary>
    /// Дополнительные слои для скрытия (настройка через YAML).
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<HumanoidVisualLayers>? AdditionalHiddenLayers;
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
