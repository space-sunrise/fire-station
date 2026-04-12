namespace Content.Shared._Scp.Fear.Components;

/// <summary>
/// Runtime-компонент активного "страха от близости".
/// Существует только пока у сущности реально активны close-fear эффекты.
/// </summary>
[RegisterComponent]
public sealed partial class ActiveCloseFearComponent : Component
{
    /// <summary>
    /// Источник страха, который сейчас применяется к сущности.
    /// </summary>
    [ViewVariables]
    public EntityUid Source;
}
