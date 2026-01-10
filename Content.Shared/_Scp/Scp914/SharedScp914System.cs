using Content.Shared.Storage;
using Content.Shared.Storage.Components;

namespace Content.Shared._Scp.Scp914;

public abstract class SharedScp914System : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Scp914ContainerComponent, StorageOpenAttemptEvent>(OnOpenAttempt);
        SubscribeLocalEvent<Scp914ContainerComponent, StorageCloseAttemptEvent>(OnCloseAttempt);
        SubscribeLocalEvent<Scp914ContainerComponent, StorageInteractAttemptEvent>(OnInteractAttempt);
    }

    private void OnCloseAttempt(Entity<Scp914ContainerComponent> ent, ref StorageCloseAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnInteractAttempt(Entity<Scp914ContainerComponent> ent, ref StorageInteractAttemptEvent args)
    {
        args.Cancelled = true;
    }

    private void OnOpenAttempt(Entity<Scp914ContainerComponent> ent, ref StorageOpenAttemptEvent args)
    {
        args.Cancelled = true;
    }

    protected Scp914Mode CycleMod(Scp914Mode value, Scp914CycleDirection direction)
    {
        var values = (Scp914Mode[])Enum.GetValues(typeof(Scp914Mode));


        var currentIndex = Array.IndexOf(values, value);

        var shift = direction == Scp914CycleDirection.Left ? -1 : 1;
        var newIndex = (currentIndex + shift) % values.Length;

        if (newIndex < 0)
        {
            newIndex += values.Length;
        }

        return values[newIndex];
    }
}
