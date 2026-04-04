using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;

namespace Content.Shared.Movement.Systems;

// Fire edit start
public abstract partial class SharedSpriteMovementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SpriteMovementComponent, SpriteMoveEvent>(OnSpriteMoveInput);

        // Fire added start
        InitializePartial();
        // Fire added end
    }

    private void OnSpriteMoveInput(Entity<SpriteMovementComponent> ent, ref SpriteMoveEvent args)
    {
        if (ent.Comp.IsMoving == args.IsMoving)
            return;

        // Fire added start
        if (ShouldBlockMovement(ent, ref args))
            return;
        // Fire added end

        ent.Comp.IsMoving = args.IsMoving;
        Dirty(ent);
    }
}
