using System.Diagnostics.CodeAnalysis;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Scp096.Mask;

public sealed class Scp096MaskSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
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
        var target = args.EquipTarget;

        // Маска должна надеваться только на сцп 096
        if (!TryComp<Scp096Component>(target, out var scp096Component))
        {
            if (_net.IsClient) // Да пососи уже, почему так
            {
                var message = Loc.GetString("scp096-mask-cannot-equip", ("name", Identity.Name(args.EquipTarget, EntityManager)));
                _popup.PopupCursor(message, args.Equipee);
            }

            args.Cancel();
            return;
        }

        // Нельзя надеть маску, пока 096 в агре
        if (scp096Component.InRageMode)
        {
            args.Cancel();
            return;
        }

        // Проигрывание звука надевания
        var equipSound = ent.Comp.EquipSound;
        if (equipSound != null)
        {
            _audio.PlayPvs(equipSound, target);
        }

        ent.Comp.SafeTimeEnd = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.SafeTime);
        Dirty(ent);
    }

    private void OnTear(Entity<Scp096Component> scp, ref Scp096TearMaskEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (!TryGetScp096Mask(scp, out var scp096Mask))
            return;

        // Нельзя снять маску, пока действует сейвтайм
        if (scp096Mask.Value.Comp.SafeTimeEnd != null && _timing.CurTime < scp096Mask.Value.Comp.SafeTimeEnd)
        {
            if (_net.IsClient) // Когда уже сделают нормальную работу попапов в шареде
            {
                var message = Loc.GetString("scp096-mask-cannot-tear-safetime", ("time", scp096Mask.Value.Comp.SafeTimeEnd - _timing.CurTime));
                _popup.PopupEntity(message, scp, scp);
            }

            return;
        }

        var doAfterArgs = new DoAfterArgs(EntityManager, scp, scp096Mask.Value.Comp.TearTime, new Scp096TearMaskDoAfterEvent(), scp, scp, scp096Mask)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
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

        var message = Loc.GetString("scp096-destroyed-mask", ("mask", MetaData(scp096Mask.Value).EntityName));
        _popup.PopupEntity(message, scp, PopupType.LargeCaution);

        QueueDel(scp096Mask);
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
