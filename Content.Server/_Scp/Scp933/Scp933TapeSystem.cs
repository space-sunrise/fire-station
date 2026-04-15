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
using Robust.Server.Audio;
using Robust.Shared.Localization;
using Robust.Shared.Physics.Events;
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

    private readonly Dictionary<EntityUid, HashSet<DoAfterId>> _targetToActiveDoAfters = new();

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
        SubscribeLocalEvent<HumanoidAppearanceComponent, MoveEvent>(OnTargetMove);
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

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return;

        RegisterDoAfterTracking(target.Owner, doAfterId.Value);

        _popup.PopupEntity(Loc.GetString("scp933-apply-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnApplyTapeDoAfter(Entity<Scp933TapeMaskComponent> tapeMask, ref Scp933ApplyTapeDoAfterEvent args)
    {
        CleanupDoAfterTracking(args.Target);

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

        if (!_doAfter.TryStartDoAfter(doAfter, out var doAfterId))
            return;

        RegisterDoAfterTracking(target.Owner, doAfterId.Value);

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
        if (args.Slot != "mask")
            return;

        EnsureComp<MutedComponent>(args.Equipee);
        tapeMask.Comp.MutedByScp933 = true;
        Dirty(tapeMask);
    }

    private void OnTapeMaskGotUnequipped(Entity<Scp933TapeMaskComponent> tapeMask, ref GotUnequippedEvent args)
    {
        if (args.Slot != "mask")
            return;

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
        CleanupDoAfterTracking(target.Owner);

        if (args.Handled || args.Cancelled)
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target.Owner, popup: false))
            return;

        TryCompleteRipTape(args.User, target, args.ExpectedMask, args.EmergencyMode);
    }

    private void OnTargetMove(Entity<HumanoidAppearanceComponent> target, ref MoveEvent args)
    {
        if (!_targetToActiveDoAfters.TryGetValue(target.Owner, out var activeDoAfters))
            return;

        foreach (var doAfterId in activeDoAfters)
        {
            _doAfter.Cancel(doAfterId);
        }

        _targetToActiveDoAfters.Remove(target.Owner);
    }

    private void RegisterDoAfterTracking(EntityUid target, DoAfterId doAfterId)
    {
        if (!_targetToActiveDoAfters.ContainsKey(target))
            _targetToActiveDoAfters[target] = new HashSet<DoAfterId>();

        _targetToActiveDoAfters[target].Add(doAfterId);
    }

    private void CleanupDoAfterTracking(EntityUid? target)
    {
        if (target == null || !_targetToActiveDoAfters.ContainsKey(target.Value))
            return;

        _targetToActiveDoAfters.Remove(target.Value);
    }

    public bool TryPeelTape(Entity<DuctTapeComponent> tape, EntityUid user, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanPeelTape(tape))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            tape.Comp.ValidatedPeelDelaySeconds,
            new Scp933PeelTapeDoAfterEvent(),
            tape,
            target: user,
            used: tape)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            NeedHand = true,
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
        return tape.Comp.UseCount != 0;
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

        if (tape.Comp.UseCount > 0)
        {
            tape.Comp.UseCount--;
            if (tape.Comp.UseCount <= 0)
            {
                QueueDel(tape);
                return;
            }

            Dirty(tape);
        }

        // Use non-positional playback until the asset is converted to mono.
        _audio.PlayGlobal(tape.Comp.PullFromRollSound, user);
        _popup.PopupEntity(Loc.GetString("scp933-peel-success"), user, user);
    }

    public bool TryApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanApplyTape(user, victim))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            tapeMask.Comp.ValidatedApplyDelaySeconds,
            new Scp933ApplyTapeDoAfterEvent(),
            tapeMask,
            target: victim,
            used: tapeMask)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            NeedHand = true,
        };

        return true;
    }

    private bool TryCompleteApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
    {
        if (!CanApplyTape(user, victim))
            return false;

        DoApplyTape(tapeMask, user, victim);
        return true;
    }

    private bool CanApplyTape(EntityUid user, EntityUid victim)
    {
        if (!HasComp<HumanoidAppearanceComponent>(victim))
            return false;

        if (!_interaction.InRangeUnobstructed(user, victim, popup: true))
            return false;

        if (TryGetScp933TapeMask(victim, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already"), user, user);
            return false;
        }

        return true;
    }

    private void DoApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
    {
        if (_inventory.TryGetSlotEntity(victim, "mask", out _))
            _inventory.TryUnequip(user, victim, "mask", silent: true, force: true);

        if (!_inventory.TryEquip(user, victim, tapeMask, "mask", silent: true, force: true))
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
            tapeMask.ValidatedRipDelaySeconds,
            new Scp933RipTapeDoAfterEvent
            {
                ExpectedMask = GetNetEntity(maskUid),
                EmergencyMode = emergencyMode,
            },
            target,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        return true;
    }

    private bool CanRipTape(EntityUid user, EntityUid target, out bool emergencyMode, out EntityUid maskUid, out Scp933TapeMaskComponent tapeMask)
    {
        emergencyMode = false;
        maskUid = default!;
        tapeMask = default!;

        var isHost = HasComp<Scp933MasterComponent>(user);

        if (!isHost)
        {
            if (_master.HasAnyScp933Host())
            {
                _popup.PopupEntity(Loc.GetString("scp933-rip-master-only"), user, user, PopupType.MediumCaution);
                return false;
            }

            if (!TryGetScp933TapeMask(target, out maskUid))
            {
                _popup.PopupEntity(Loc.GetString("scp933-rip-master-only"), user, user, PopupType.MediumCaution);
                return false;
            }

            if (!TryComp<Scp933TapeMaskComponent>(maskUid, out var tapeMaskTemp))
            {
                _popup.PopupEntity(Loc.GetString("scp933-rip-master-only"), user, user, PopupType.MediumCaution);
                return false;
            }
            tapeMask = tapeMaskTemp!;

            if (!tapeMask.EmergencyRipAvailable)
            {
                _popup.PopupEntity(Loc.GetString("scp933-rip-master-only"), user, user, PopupType.MediumCaution);
                return false;
            }

            emergencyMode = true;
        }
        else
        {
            if (!TryGetScp933TapeMask(target, out maskUid))
                return false;

            if (!TryComp<Scp933TapeMaskComponent>(maskUid, out var tapeMaskTemp))
                return false;
            tapeMask = tapeMaskTemp!;
        }

        if (!_interaction.InRangeUnobstructed(user, target, popup: true))
            return false;

        return true;
    }

    private bool TryCompleteRipTape(EntityUid user, EntityUid target, NetEntity expectedMask, bool emergencyMode)
    {
        if (!CanRipTape(target, expectedMask, out var tapeMask))
            return false;

        var isHost = HasComp<Scp933MasterComponent>(user);
        var actualEmergencyMode = !isHost && tapeMask.EmergencyRipAvailable;

        DoRipTape(user, target, tapeMask, actualEmergencyMode);
        return true;
    }

    private bool CanRipTape(EntityUid target, NetEntity expectedMask, out Scp933TapeMaskComponent tapeMask)
    {
        tapeMask = default!;

        var expectedMaskUid = GetEntity(expectedMask);
        if (expectedMaskUid == null)
            return false;

        if (!TryGetScp933TapeMask(target, out var currentMask))
            return false;

        if (currentMask != expectedMaskUid)
            return false;

        if (!TryComp<Scp933TapeMaskComponent>(currentMask, out var currentTapeMask))
            return false;

        tapeMask = currentTapeMask;
        return true;
    }

    private void DoRipTape(EntityUid user, EntityUid target, Scp933TapeMaskComponent tapeMask, bool emergencyMode)
    {
        tapeMask.RitualUnequipAllowed = true;
        tapeMask.RitualUnequipUser = user;
        tapeMask.RitualAllowNonHost = emergencyMode;

        var unequipped = _inventory.TryUnequip(user, target, "mask", out var removed, silent: true, force: true);

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

    private bool TryGetScp933TapeMask(EntityUid uid, out EntityUid maskUid)
    {
        maskUid = default;

        if (!_inventory.TryGetSlotEntity(uid, "mask", out var entity))
            return false;

        if (!HasComp<Scp933TapeMaskComponent>(entity))
            return false;

        maskUid = entity.Value;
        return true;
    }
}
