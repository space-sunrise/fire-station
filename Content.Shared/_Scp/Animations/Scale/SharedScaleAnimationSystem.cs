using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Animations.Scale;

/// <summary>
/// Система, обрабатывающая анимацию увеличения размера спрайта при появлении сущности.
/// Основывается на <see cref="ScaleAnimationComponent"/>
/// </summary>
public abstract class SharedScaleAnimationSystem : EntitySystem
{
    [Dependency] protected readonly IGameTiming Timing = default!;
}

[Serializable, NetSerializable]
public sealed class ScaleAnimationStartEvent(NetEntity entity) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
}
