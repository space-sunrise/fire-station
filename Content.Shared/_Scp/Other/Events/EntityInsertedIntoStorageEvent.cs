namespace Content.Shared._Scp.Other.Events;

public sealed class EntityInsertedIntoStorageEvent(EntityUid storage, EntityUid? user) : EntityEventArgs
{
    public readonly EntityUid Storage = storage;
    public readonly EntityUid? User = user;
}

public sealed class EntityRemovedFromStorageEvent(EntityUid storage, EntityUid user) : EntityEventArgs
{
    public readonly EntityUid Storage = storage;
    public readonly EntityUid User = user;
}
