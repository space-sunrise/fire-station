using Content.Shared._Scp.Scp9332;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;

namespace Content.Server._Scp.Scp9332;

/// <summary>
/// Серверная часть системы SCP-933-2.
/// Обрабатывает логику поведения рабов, реагирует на команды.
/// </summary>
public sealed class Scp9332System : SharedScp9332System
{
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly NpcFactionSystem _faction = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Подписываемся на команды действий (Actions)
        SubscribeLocalEvent<Scp9332ControllerComponent, Scp9332OrderStayActionEvent>(OnOrderStay);
        SubscribeLocalEvent<Scp9332ControllerComponent, Scp9332OrderFollowActionEvent>(OnOrderFollow);
        SubscribeLocalEvent<Scp9332ControllerComponent, Scp9332OrderAttackActionEvent>(OnOrderAttack);
        SubscribeLocalEvent<Scp9332ControllerComponent, Scp9332FreeAllSlavesActionEvent>(OnFreeAllSlaves);
    }

    /// <summary>
    /// Команда "Стоять" - рабы должны остановиться.
    /// </summary>
    private void OnOrderStay(Entity<Scp9332ControllerComponent> ent, ref Scp9332OrderStayActionEvent args)
    {
        if (args.Performer != ent.Owner)
            return;

        args.Handled = true;
        SetSlaveOrder(ent.Owner, SlaveOrderType.Stay);
    }

    /// <summary>
    /// Команда "Следовать" - рабы должны следовать за контроллёром.
    /// </summary>
    private void OnOrderFollow(Entity<Scp9332ControllerComponent> ent, ref Scp9332OrderFollowActionEvent args)
    {
        if (args.Performer != ent.Owner)
            return;

        args.Handled = true;
        SetSlaveOrder(ent.Owner, SlaveOrderType.Follow);
    }

    /// <summary>
    /// Команда "Атаковать" - рабы должны атаковать врагов.
    /// </summary>
    private void OnOrderAttack(Entity<Scp9332ControllerComponent> ent, ref Scp9332OrderAttackActionEvent args)
    {
        if (args.Performer != ent.Owner)
            return;

        args.Handled = true;
        SetSlaveOrder(ent.Owner, SlaveOrderType.Attack);
    }

    /// <summary>
    /// Освобождение всех рабов.
    /// </summary>
    private void OnFreeAllSlaves(Entity<Scp9332ControllerComponent> ent, ref Scp9332FreeAllSlavesActionEvent args)
    {
        if (args.Performer != ent.Owner)
            return;

        args.Handled = true;

        var slaves = new List<EntityUid>(ent.Comp.Slaves);
        foreach (var slave in slaves)
        {
            FreeSlave(slave);
        }
    }
}
