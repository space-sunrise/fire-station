using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Scp.Trigger.UpdateAppearanceOnTrigger;

/// <summary>
/// Internal marker used by <see cref="UpdateAppearanceOnTriggerSystem"/> to process only entities that are currently
/// waiting for an appearance reset.
/// </summary>
/// <remarks>
/// This is a pure performance helper.
/// Instead of iterating over every entity with <see cref="UpdateAppearanceOnTriggerComponent"/> every frame,
/// the system only iterates over entities that currently have a pending timed reset.
/// </remarks>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(UpdateAppearanceOnTriggerSystem))]
public sealed partial class ActiveUpdateAppearanceOnTriggerComponent : Component
{
    /// <summary>
    /// Absolute game time when the pending reset should be performed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan ResetAt = TimeSpan.Zero;

    /// <summary>
    /// Entity whose appearance should be restored when <see cref="ResetAt"/> is reached.
    /// </summary>
    [ViewVariables]
    public EntityUid? ResetTarget;
}
