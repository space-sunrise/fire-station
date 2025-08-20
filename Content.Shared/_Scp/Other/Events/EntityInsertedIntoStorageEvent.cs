namespace Content.Shared._Scp.Other.Events;

public sealed class EntityInsertedIntoStorageEvent(EntityUid storage) : EntityEventArgs
{
    public readonly EntityUid Storage = storage;
}

public sealed class EntityRemovedFromStorageEvent(EntityUid storage) : EntityEventArgs
{
    public readonly EntityUid Storage = storage;
}
