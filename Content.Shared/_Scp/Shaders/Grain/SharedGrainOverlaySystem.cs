using Robust.Shared.Network;

namespace Content.Shared._Scp.Shaders.Grain;

public sealed class SharedGrainOverlaySystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    /// <summary>
    /// Устанавливает дополнительную силу шейдера зернистости: <see cref="GrainOverlayComponent.AdditionalStrength"/> .
    /// </summary>
    /// <param name="ent">Сущность, к которой будет применено значение</param>
    /// <param name="value">Значение, которое будет установлено</param>
    /// <returns>Получилось/Не получилось</returns>
    public bool TrySetAdditionalStrength(Entity<GrainOverlayComponent?> ent, int value)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        ent.Comp.AdditionalStrength = value;

        if (_net.IsServer)
            DirtyField(ent, nameof(GrainOverlayComponent.AdditionalStrength));

        return true;
    }
}
