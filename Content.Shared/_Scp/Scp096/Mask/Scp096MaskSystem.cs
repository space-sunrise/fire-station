using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096.Mask;

public sealed class Scp096MaskSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const string HeadSlot = "head";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096MaskComponent, BeingEquippedAttemptEvent>(OnEquip);

        SubscribeLocalEvent<Scp096Component, Scp096TearMaskEvent>(OnTear);
        SubscribeLocalEvent<Scp096Component, Scp096TearMaskDoAfterEvent>(OnTearSuccess);
    }

    private void OnEquip(Entity<Scp096MaskComponent> ent, ref BeingEquippedAttemptEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var target = args.EquipTarget;

        // Маска должна надеваться только на сцп 096
        if (!HasComp<Scp096Component>(target))
        {
            _popup.PopupEntity("Маска слишком велика для ", args.EquipTarget, args.Equipee);

            args.Cancel();
        }

        // Проигрывание звука надевания
        var equipSound = ent.Comp.EquipSound;
        if (equipSound != null)
        {
            _audio.PlayPvs(equipSound, target);
        }
    }

    private void OnTearSuccess(Entity<Scp096Component> scp, ref Scp096TearMaskDoAfterEvent args)
    {
        if (!TryGetScp096Mask(scp, out var scp096Mask))
            return;

        // Звук рвущейся маски
        var tearSound = scp096Mask.Value.Comp.TearSound;
        if (tearSound != null)
        {
            _audio.PlayPvs(tearSound,scp);
        }

        QueueDel(scp096Mask);
    }

    private void OnTear(Entity<Scp096Component> scp, ref Scp096TearMaskEvent args)
    {
        if (!TryGetScp096Mask(scp, out var scp096Mask))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, scp, scp096Mask.Value.Comp.TearTime, new Scp096TearMaskDoAfterEvent(), scp, scp, scp096Mask)
        {
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    /// <summary>
    /// Метод, позволяющий проверить, надета ли на сцп 096 маска. В случае привета передает ентити маски
    /// </summary>
    /// <param name="scp">Сцп 096</param>
    /// <param name="mask">Маска сцп 096</param>
    /// <returns></returns>
    public bool TryGetScp096Mask(Entity<Scp096Component> scp, [NotNullWhen(true)] out Entity<Scp096MaskComponent>? mask)
    {
        mask = null;

        if (!_inventory.TryGetSlotEntity(scp, HeadSlot, out var headEntity))
            return false;

        if (!TryComp<Scp096MaskComponent>(headEntity, out var scp096MaskComponent))
            return false;

        mask = (headEntity.Value, scp096MaskComponent);

        return true;
    }

}

public sealed partial class Scp096TearMaskEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class Scp096TearMaskDoAfterEvent : SimpleDoAfterEvent;
