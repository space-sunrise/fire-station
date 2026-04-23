using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared._Scp.Scp933;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Stacks;
using Robust.Server.Audio;
using System.Linq;
using Robust.Shared.Localization;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Серверная логика ленты SCP-933:
/// отрыв полоски от рулона -> наклейка -> срыв.
/// </summary>
public sealed class Scp933TapeSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly Scp933MasterSystem _master = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DuctTapeComponent, UseInHandEvent>(OnDuctTapeUseInHand);
        SubscribeLocalEvent<DuctTapeComponent, Scp933PeelTapeDoAfterEvent>(OnPeelTapeDoAfter);
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractUsingEvent>(OnHumanoidInteractUsing);
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractHandEvent>(OnHumanoidInteractHand);
        SubscribeLocalEvent<Scp933TapeMaskComponent, Scp933ApplyTapeDoAfterEvent>(OnApplyTapeDoAfter);
        SubscribeLocalEvent<Scp933TapeMaskComponent, GotEquippedEvent>(OnTapeMaskGotEquipped);
        SubscribeLocalEvent<Scp933TapeMaskComponent, GotUnequippedEvent>(OnTapeMaskGotUnequipped);
        SubscribeLocalEvent<Scp933TapeMaskComponent, BeingUnequippedAttemptEvent>(OnTapeBeingUnequippedAttempt);
        SubscribeLocalEvent<HumanoidAppearanceComponent, Scp933RipTapeDoAfterEvent>(OnRipTapeDoAfter);
    }

    private void OnDuctTapeUseInHand(Entity<DuctTapeComponent> tape, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryPeelTape(tape, args.User, out var doAfter))
            return;

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-peel-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnPeelTapeDoAfter(Entity<DuctTapeComponent> tape, ref Scp933PeelTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        TryCompletePeelTape(tape, args.User);
    }

    private void OnHumanoidInteractUsing(Entity<HumanoidAppearanceComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<Scp933TapeMaskComponent>(args.Used, out var tapeMask))
            return;

        if (!TryApplyTape((args.Used, tapeMask), args.User, target.Owner, out var doAfter))
            return;

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-apply-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnApplyTapeDoAfter(Entity<Scp933TapeMaskComponent> tapeMask, ref Scp933ApplyTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (args.Target is not { } victim)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, victim, popup: false))
            return;

        TryCompleteApplyTape(tapeMask, args.User, victim);
    }

    private void OnHumanoidInteractHand(Entity<HumanoidAppearanceComponent> target, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!TryRipTape(args.User, target.Owner, out var doAfter))
            return;

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-rip-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnTapeBeingUnequippedAttempt(Entity<Scp933TapeMaskComponent> tape, ref BeingUnequippedAttemptEvent args)
    {
        if (!tape.Comp.RitualUnequipAllowed)
        {
            args.Cancel();
            return;
        }

        if (tape.Comp.RitualUnequipUser != args.Unequipee)
        {
            args.Cancel();
            return;
        }

        if (HasComp<Scp933MasterComponent>(args.Unequipee))
            return;

        if (tape.Comp.RitualAllowNonHost)
            return;

        args.Cancel();
    }

    private void OnTapeMaskGotEquipped(Entity<Scp933TapeMaskComponent> tapeMask, ref GotEquippedEvent args)
    {
        if (!tapeMask.Comp.EquipSlots.Contains(args.Slot))
            return;

        EnsureComp<MutedComponent>(args.Equipee);
        EnsureComp<TapedFaceComponent>(args.Equipee);
        tapeMask.Comp.MutedByScp933 = true;
        Dirty(tapeMask);
    }

    private void OnTapeMaskGotUnequipped(Entity<Scp933TapeMaskComponent> tapeMask, ref GotUnequippedEvent args)
    {
        if (!tapeMask.Comp.EquipSlots.Contains(args.Slot))
            return;

        RemComp<TapedFaceComponent>(args.Equipee);

        if (HasComp<Scp933FaceTornComponent>(args.Equipee))
            return;

        if (tapeMask.Comp.MutedByScp933)
        {
            RemComp<MutedComponent>(args.Equipee);
            tapeMask.Comp.MutedByScp933 = false;
            Dirty(tapeMask);
        }
    }

    private void OnRipTapeDoAfter(Entity<HumanoidAppearanceComponent> target, ref Scp933RipTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target.Owner, popup: false))
            return;

        TryCompleteRipTape(args.User, target, args.ExpectedMask, args.EmergencyMode);
    }

    public bool TryPeelTape(Entity<DuctTapeComponent> tape, EntityUid user, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanPeelTape(tape))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            tape.Comp.PeelDelaySeconds,
            new Scp933PeelTapeDoAfterEvent(),
            tape,
            target: user,
            used: tape)
        {
            BreakOnMove = tape.Comp.BreakOnMove,
            BreakOnDamage = tape.Comp.BreakOnDamage,
            BreakOnDropItem = tape.Comp.BreakOnDropItem,
            BreakOnHandChange = tape.Comp.BreakOnHandChange,
            NeedHand = tape.Comp.NeedHand,
        };

        return true;
    }

    private bool TryCompletePeelTape(Entity<DuctTapeComponent> tape, EntityUid user)
    {
        if (!CanPeelTape(tape))
            return false;

        DoPeelTape(tape, user);
        return true;
    }

    private bool CanPeelTape(Entity<DuctTapeComponent> tape)
    {
        if (!TryComp<StackComponent>(tape.Owner, out var stack))
            return true;

        return stack.Count > 0;
    }

    private void DoPeelTape(Entity<DuctTapeComponent> tape, EntityUid user)
    {
        var peel = Spawn(tape.Comp.TapeMaskPrototype);

        if (!_hands.TryPickupAnyHand(user, peel))
        {
            QueueDel(peel);
            _popup.PopupEntity(Loc.GetString("scp933-peel-hand-fail"), user, user, PopupType.MediumCaution);
            return;
        }

        if (TryComp<StackComponent>(tape.Owner, out var stack))
        {
            if (stack.Count > 1)
            {
                stack.Count--;
                Dirty(tape.Owner, stack);
            }
            else
            {
                QueueDel(tape);
                return;
            }
        }

        // Use non-positional playback until the asset is converted to mono.
        _audio.PlayGlobal(tape.Comp.PullFromRollSound, user);
        _popup.PopupEntity(Loc.GetString("scp933-peel-success"), user, user);
    }

    public bool TryApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanApplyTape(tapeMask, user, victim))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            tapeMask.Comp.ApplyDelaySeconds,
            new Scp933ApplyTapeDoAfterEvent(),
            tapeMask,
            target: victim,
            used: tapeMask)
        {
            BreakOnMove = tapeMask.Comp.BreakOnUserMove,
            BreakOnDamage = tapeMask.Comp.BreakOnDamage,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            NeedHand = tapeMask.Comp.NeedHand,
        };

        return true;
    }

    private bool TryCompleteApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
    {
        if (!CanApplyTape(tapeMask, user, victim))
            return false;

        DoApplyTape(tapeMask, user, victim);
        return true;
    }

    private bool CanApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
    {
        if (!TryComp<Scp933PossibleTargetComponent>(victim, out var targetComp) || !targetComp.CanWearTape)
            return false;

        if (!_interaction.InRangeUnobstructed(user, victim, popup: true))
            return false;

        if (TryGetScp933TapeMask(victim, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already"), user, user);
            return false;
        }

        var slot = tapeMask.Comp.EquipSlots.FirstOrDefault();
        if (!string.IsNullOrEmpty(slot) && _inventory.TryGetSlotEntity(victim, slot, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-equip-fail"), user, user, PopupType.MediumCaution);
            return false;
        }

        return true;
    }

    private void DoApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
    {
        var slot = tapeMask.Comp.EquipSlots.FirstOrDefault();
        if (string.IsNullOrEmpty(slot))
            return;

        if (!_inventory.TryEquip(user, victim, tapeMask, slot, silent: true))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-equip-fail"), user, user, PopupType.MediumCaution);
            return;
        }

        _audio.PlayGlobal(tapeMask.Comp.ApplyToFaceSound, victim);
        _popup.PopupEntity(Loc.GetString("scp933-apply-success-user"), user, user);
        _popup.PopupEntity(Loc.GetString("scp933-apply-success-target"), victim, victim, PopupType.MediumCaution);
    }

    public bool TryRipTape(EntityUid user, EntityUid target, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanRipTape(user, target, out var emergencyMode, out var maskUid, out var tapeMask))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            tapeMask.RipDelaySeconds,
            new Scp933RipTapeDoAfterEvent
            {
                ExpectedMask = GetNetEntity(maskUid),
                EmergencyMode = emergencyMode,
            },
            target,
            target: target)
        {
            BreakOnMove = tapeMask.BreakOnUserMove,
            BreakOnDamage = tapeMask.BreakOnDamage,
            NeedHand = tapeMask.NeedHand,
        };

        return true;
    }

    private bool CanRipTape(EntityUid user, EntityUid target, out bool emergencyMode, out EntityUid maskUid, out Scp933TapeMaskComponent tapeMask)
    {
        emergencyMode = false;
        maskUid = default!;
        tapeMask = default!;

        if (!TryComp<Scp933PossibleTargetComponent>(target, out var targetComp) || !targetComp.CanBeFaceTorn)
            return false;

        // Сначала проверяем есть ли лента на цели - если нет, просто молча выходим (не мешаем обычным взаимодействиям)
        if (!TryGetScp933TapeMask(target, out maskUid, out var tapeMaskTemp) || tapeMaskTemp == null)
            return false;
        tapeMask = tapeMaskTemp;

        var isHost = HasComp<Scp933MasterComponent>(user);

        if (!isHost)
        {
            var ripMasterOnlyMsg = "scp933-rip-master-only";

            if (_master.HasAnyScp933Host())
            {
                _popup.PopupEntity(Loc.GetString(ripMasterOnlyMsg), user, user, PopupType.MediumCaution);
                return false;
            }

            if (!tapeMask.EmergencyRipAvailable)
            {
                _popup.PopupEntity(Loc.GetString(ripMasterOnlyMsg), user, user, PopupType.MediumCaution);
                return false;
            }

            emergencyMode = true;
        }

        if (!_interaction.InRangeUnobstructed(user, target, popup: true))
            return false;

        return true;
    }

    private bool TryCompleteRipTape(EntityUid user, EntityUid target, NetEntity expectedMask, bool emergencyMode)
    {
        if (!CanRipTape(target, expectedMask, out var maskUid, out var tapeMask))
            return false;

        var isHost = HasComp<Scp933MasterComponent>(user);
        var actualEmergencyMode = !isHost && tapeMask.EmergencyRipAvailable;

        DoRipTape(user, target, maskUid, tapeMask, actualEmergencyMode);
        return true;
    }

    private bool CanRipTape(EntityUid target, NetEntity expectedMask, out EntityUid maskUid, out Scp933TapeMaskComponent tapeMask)
    {
        maskUid = default!;
        tapeMask = default!;

        var expectedMaskUid = GetEntity(expectedMask);
        if (expectedMaskUid == null)
            return false;

        if (!TryGetScp933TapeMask(target, out var currentMask, out _))
            return false;

        if (currentMask != expectedMaskUid)
            return false;

        if (!TryComp<Scp933TapeMaskComponent>(currentMask, out var currentTapeMask))
            return false;

        maskUid = currentMask;
        tapeMask = currentTapeMask;
        return true;
    }

    private void DoRipTape(EntityUid user, EntityUid target, EntityUid maskUid, Scp933TapeMaskComponent tapeMask, bool emergencyMode)
    {
        tapeMask.RitualUnequipAllowed = true;
        tapeMask.RitualUnequipUser = user;
        tapeMask.RitualAllowNonHost = emergencyMode;
        var slot = tapeMask.EquipSlots.FirstOrDefault(s => _inventory.TryGetSlotEntity(target, s, out var slotEntity) && slotEntity == maskUid);
        if (string.IsNullOrEmpty(slot))
            slot = tapeMask.EquipSlots.FirstOrDefault();

        if (string.IsNullOrEmpty(slot))
            return;

        var unequipped = _inventory.TryUnequip(user, target, slot, out var removed, silent: true, force: true);

        tapeMask.RitualUnequipAllowed = false;
        tapeMask.RitualUnequipUser = null;
        tapeMask.RitualAllowNonHost = false;

        if (!unequipped)
            return;

        if (emergencyMode)
            tapeMask.EmergencyRipAvailable = false;

        if (removed != null)
            QueueDel(removed.Value);

        if (emergencyMode || !_master.HasAnyScp933Host())
        {
            _master.ConvertToMaster(target);
            _master.ApplyHostBuffs(target);
        }
        else
        {
            _master.ApplyFaceTornAfterRip(user, target);
        }

        _audio.PlayGlobal(tapeMask.RipFromFaceSound, target);
        _popup.PopupEntity(Loc.GetString("scp933-rip-success-user"), user, user);
        _popup.PopupEntity(Loc.GetString("scp933-rip-success-target"), target, target, PopupType.MediumCaution);
    }

    private bool TryGetScp933TapeMask(EntityUid uid, out EntityUid maskUid, out Scp933TapeMaskComponent? tapeMaskComp)
    {
        maskUid = default;
        tapeMaskComp = null;

        if (_inventory.TryGetContainerSlotEnumerator(uid, out var enumerator))
        {
            while (enumerator.MoveNext(out var slot))
            {
                if (slot.ContainedEntity is not { } item)
                    continue;

                if (TryComp<Scp933TapeMaskComponent>(item, out var tapeComp) && tapeComp.EquipSlots.Contains(slot.ID))
                {
                    maskUid = item;
                    tapeMaskComp = tapeComp;
                    return true;
                }
            }
        }

        return false;
    }
}
