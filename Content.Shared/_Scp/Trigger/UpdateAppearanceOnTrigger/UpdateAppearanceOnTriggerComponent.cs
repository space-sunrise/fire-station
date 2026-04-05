using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared._Scp.Trigger.UpdateAppearanceOnTrigger;

/// <summary>
/// A typed YAML wrapper for values written by <see cref="UpdateAppearanceOnTriggerComponent"/>.
/// </summary>
/// <remarks>
/// <para>
/// This type exists because the trigger effect needs to support several practical value types for
/// <see cref="AppearanceComponent"/> and <c>GenericVisualizer</c>:
/// </para>
/// <list type="bullet">
/// <item>
/// <description><see cref="Bool"/> for simple true/false visual states.</description>
/// </item>
/// <item>
/// <description><see cref="String"/> for named states such as <c>open</c> / <c>closed</c>.</description>
/// </item>
/// <item>
/// <description><see cref="Enum"/> for strongly typed appearance states.</description>
/// </item>
/// </list>
/// <para>
/// Only one field should be filled in normal use.
/// If several are set at once, resolution order is <see cref="Enum"/> -&gt; <see cref="String"/> -&gt; <see cref="Bool"/>.
/// </para>
/// </remarks>
/// <example>
/// Boolean value:
/// <code>
/// value:
///   bool: true
/// </code>
/// </example>
/// <example>
/// String value:
/// <code>
/// value:
///   string: open
/// </code>
/// </example>
/// <example>
/// Enum value:
/// <code>
/// value:
///   enum: enum.ScpBookVisualState.Open
/// </code>
/// </example>
[DataDefinition]
public sealed partial class UpdateAppearanceOnTriggerValue
{
    /// <summary>
    /// Boolean appearance value.
    /// Useful for visualizers that react to <c>True</c> / <c>False</c>.
    /// </summary>
    [DataField]
    public bool? Bool;

    /// <summary>
    /// String appearance value.
    /// Useful for named visual states such as <c>open</c>, <c>closed</c>, <c>idle</c>, or <c>alarm</c>.
    /// </summary>
    [DataField]
    public string? String;

    /// <summary>
    /// Enum appearance value.
    /// Useful when the appearance state is already modeled as a dedicated enum.
    /// </summary>
    [DataField(customTypeSerializer: typeof(EnumSerializer))]
    public Enum? Enum;

    /// <summary>
    /// Resolves the configured value into a runtime object that can be passed to
    /// <see cref="SharedAppearanceSystem.SetData(EntityUid, Enum, object, AppearanceComponent?)"/>.
    /// </summary>
    /// <param name="value">The resolved runtime value.</param>
    /// <returns>
    /// <see langword="true"/> if one of the supported fields was configured;
    /// otherwise <see langword="false"/>.
    /// </returns>
    public bool TryGetValue([NotNullWhen(true)] out object? value)
    {
        if (Enum != null)
        {
            value = Enum;
            return true;
        }

        if (String != null)
        {
            value = String;
            return true;
        }

        if (Bool != null)
        {
            value = Bool.Value;
            return true;
        }

        value = null;
        return false;
    }
}

/// <summary>
/// Trigger effect that writes an appearance value to an entity, allowing its visuals to be changed through
/// <see cref="AppearanceComponent"/> and <c>GenericVisualizer</c>.
/// </summary>
/// <remarks>
/// <para>
/// This component is intended to be the visual counterpart to trigger sources such as
/// <c>TriggerOnSignalSwitch</c>. When the entity receives a matching <see cref="TriggerEvent"/>, the effect:
/// </para>
/// <list type="number">
/// <item>
/// <description>Writes <see cref="Value"/> into the appearance data under <see cref="Key"/>.</description>
/// </item>
/// <item>
/// <description>
/// Optionally starts a timed reset if <see cref="Duration"/> is greater than zero.
/// </description>
/// </item>
/// <item>
/// <description>Restores <see cref="ResetValue"/> after the timer ends.</description>
/// </item>
/// </list>
/// <para>
/// The target entity is controlled by <see cref="BaseXOnTriggerComponent.TargetUser"/>:
/// if it is <see langword="false"/>, the appearance is changed on the owner of this component;
/// if it is <see langword="true"/>, the appearance is changed on the trigger user.
/// </para>
/// <para>
/// <see cref="Key"/> must remain an <see cref="Enum"/>. This is an engine limitation:
/// <see cref="AppearanceComponent"/> stores appearance data as <c>Dictionary&lt;Enum, object&gt;</c>,
/// and <see cref="SharedAppearanceSystem.SetData(EntityUid, Enum, object, AppearanceComponent?)"/> also requires
/// an enum key. Because of that, string values are supported, but string keys are not.
/// </para>
/// </remarks>
/// <example>
/// Boolean state for a pressed button:
/// <code>
/// - type: UpdateAppearanceOnTrigger
///   key: enum.EmergencyRadioButtonVisuals.Pressed
///   value:
///     bool: true
///   resetValue:
///     bool: false
///   duration: 0.4
/// </code>
/// This matches a GenericVisualizer mapping that reacts to <c>True</c> and <c>False</c>.
/// </example>
/// <example>
/// String state for named visuals:
/// <code>
/// - type: UpdateAppearanceOnTrigger
///   key: enum.SomeVisuals.State
///   value:
///     string: open
///   resetValue:
///     string: closed
///   duration: 0.4
/// </code>
/// This matches a GenericVisualizer mapping that reacts to <c>open</c> and <c>closed</c>.
/// </example>
/// <example>
/// Enum state for strongly typed visual states:
/// <code>
/// - type: UpdateAppearanceOnTrigger
///   key: enum.ScpBookVisualLayers.ScpBookVisualState
///   value:
///     enum: enum.ScpBookVisualState.Open
///   resetValue:
///     enum: enum.ScpBookVisualState.Closed
///   duration: 0.4
/// </code>
/// This matches a GenericVisualizer mapping that reacts to the enum names <c>Open</c> and <c>Closed</c>.
/// </example>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(UpdateAppearanceOnTriggerSystem))]
public sealed partial class UpdateAppearanceOnTriggerComponent : BaseXOnTriggerComponent
{
    /// <summary>
    /// Appearance key that will be updated when the trigger fires.
    /// </summary>
    /// <remarks>
    /// This must be an enum reference such as <c>enum.EmergencyRadioButtonVisuals.Pressed</c>.
    /// String keys are not supported by the appearance system.
    /// </remarks>
    [DataField(required: true, customTypeSerializer: typeof(EnumSerializer))]
    public Enum Key = default!;

    /// <summary>
    /// Value written to <see cref="Key"/> when the trigger is activated.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>bool: true</c>, which makes simple press-state use cases concise.
    /// </remarks>
    [DataField]
    public UpdateAppearanceOnTriggerValue Value = new() { Bool = true };

    /// <summary>
    /// Value written back to <see cref="Key"/> after <see cref="Duration"/> elapses.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>bool: false</c>.
    /// </remarks>
    [DataField]
    public UpdateAppearanceOnTriggerValue ResetValue = new() { Bool = false };

    /// <summary>
    /// How long <see cref="Value"/> should stay active before the system restores <see cref="ResetValue"/>.
    /// </summary>
    /// <remarks>
    /// If this is zero or less, the component applies <see cref="Value"/> and does not schedule a timed reset.
    /// </remarks>
    [DataField]
    public TimeSpan Duration = TimeSpan.Zero;
}
