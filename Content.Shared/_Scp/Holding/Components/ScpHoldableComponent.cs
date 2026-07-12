using Content.Shared._Scp.Other.WorldAlert;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Holding.Components;

/// <summary>
/// Marks an entity as a valid target for the SCP holding mechanic and stores per-target hold tuning.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ScpHoldableComponent : Component
{
    /// <summary>
    /// Optional whitelist of entities that may hold this target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? HolderWhitelist;

    /// <summary>
    /// Optional blacklist of entities that may not hold this target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? HolderBlacklist;

    /// <summary>
    /// Number of hands each holder must reserve to contribute to holding this target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int HolderHandsRequired = 1;

    /// <summary>
    /// Minimum uninterrupted full hold duration before a breakout do-after may start.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FullHoldDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Duration of the visible breakout do-after for a full hold.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan FullBreakoutDuration = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Duration of immunity after a successful full breakout.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan PostBreakoutImmunity = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Optional visual and audio feedback played when a full-hold breakout attempt starts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public WorldAlertSettings BreakoutAttemptAlertSettings = new()
    {
        Prototype = "WorldAlertHandcuffs",
        Sound = new SoundCollectionSpecifier("storageRustle",
            AudioParams.Default.WithVolume(-2f).WithMaxDistance(4f).WithVariation(0.15f)),
        DirectSound = true,
    };

    /// <summary>
    /// Maximum unobstructed range allowed between holder and target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HoldRange = 1f;

    /// <summary>
    /// Scales the preferred soft-drag distance from the configured hold range.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragDistanceFactor = 0.3f;

    /// <summary>
    /// Lower clamp for the preferred soft-drag distance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragMinimumDistance = 0.4f;

    /// <summary>
    /// Upper clamp for the preferred soft-drag distance.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragMaximumDistance = 0.6f;

    /// <summary>
    /// Distance where the system snaps to the holder-facing direction instead of offset.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragSnapTolerance = 0.03f;

    /// <summary>
    /// Distance where the held target is considered settled and only matches holder velocity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragSettleTolerance = 0.08f;

    /// <summary>
    /// Minimum velocity used to derive drag direction from holder movement.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragVelocityDirectionThreshold = 0.05f;

    /// <summary>
    /// Minimum time window used to catch the held target back up to its desired position.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragCatchUpTime = 0.05f;

    /// <summary>
    /// Maximum correction speed applied while soft-dragging the held target.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragMaximumCorrectionSpeed = 6f;

    /// <summary>
    /// Extra correction strength applied when the held target moves away from its desired position.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragAwayVelocityStrength = 0.6f;

    /// <summary>
    /// Velocity difference threshold before the held body's velocity is updated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SoftDragVelocityTolerance = 0.05f;

    /// <summary>
    /// Walk speed modifier applied to holders while they move this target.
    /// Lower values make the target heavier to move.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HolderWalkModifier = 0.7f;

    /// <summary>
    /// Sprint speed modifier applied to holders while they move this target.
    /// Lower values make the target heavier to move.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float HolderSprintModifier = 0.7f;

    /// <summary>
    /// Optional speed modifier applied only while the held target is moved toward the cursor.
    /// If not set, <see cref="HolderSprintModifier"/> is used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? CursorMoveSpeedModifier;
}
