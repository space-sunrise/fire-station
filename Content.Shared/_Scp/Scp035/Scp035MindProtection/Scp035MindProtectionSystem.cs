using Robust.Shared.Containers;

namespace Content.Shared._Scp.Scp035.Scp035MindProtection;

public sealed class Scp035MindProtectionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp035MaskComponent, ContainerGettingInsertedAttemptEvent>(OnEquipAttempt);
    }

    /// <summary>
    /// Не дает защищенному игроку взять/надеть SCP-035
    /// </summary>
    /// <param name="scp">Защищенный игрок</param>
    /// <param name="args">Ивент</param>
    private void OnEquipAttempt(Entity<Scp035MaskComponent> scp, ref ContainerGettingInsertedAttemptEvent args)
    {
        if (HasComp<Scp035MindProtectionComponent>(args.Container.Owner))
            args.Cancel();
    }
}
