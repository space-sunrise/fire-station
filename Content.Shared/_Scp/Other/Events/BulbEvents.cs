namespace Content.Shared._Scp.Other.Events;

public sealed class LightEjectEvent(EntityUid bulb) : EntityEventArgs
{
    public EntityUid Bulb = bulb;
}

public sealed class LightInsertEvent(EntityUid bulb) : EntityEventArgs
{
    public EntityUid Bulb = bulb;
}
