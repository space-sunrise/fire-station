namespace Content.Shared._Scp.Other.Events;

public sealed class AirlockCrushedEvent(NetEntity entity) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
}
