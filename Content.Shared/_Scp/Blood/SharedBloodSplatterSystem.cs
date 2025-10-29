using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Blood;

public abstract class SharedBloodSplatterSystem : EntitySystem
{

}

[Serializable, NetSerializable]
public sealed class BloodParticleAnimationStartEvent(NetEntity entity) : EntityEventArgs
{
    public readonly NetEntity Entity = entity;
}
