using Content.Shared.Interaction.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

public abstract partial class SharedSpriteMovementSystem
{
    private EntityQuery<BlockMovementComponent> _blockQuery;

    private void InitializePartial()
    {
        _blockQuery = GetEntityQuery<BlockMovementComponent>();
    }

    private bool ShouldBlockMovement(Entity<SpriteMovementComponent> ent, ref SpriteMoveEvent args)
    {
        if (_blockQuery.HasComp(ent))
            return true;

        var ev = new SpriteMovementAttemptEvent();
        RaiseLocalEvent(ent, ref ev);

        if (ev.Cancelled)
            return true;

        return false;
    }
}

[ByRefEvent]
public record struct SpriteMovementAttemptEvent
{
    public bool Cancelled;
}
