using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Interaction;
using Content.Server.Popups;
using Content.Shared._Scp.Scp933;
using Content.Shared.DoAfter;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech.Muting;
using Content.Shared.Weapons.Melee;
using Robust.Server.Audio;
using Robust.Shared.Localization;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Сервер: полный цикл ленты SCP-933 с do-after:
/// отрыв полоски от рулона -> наклеивание полоски -> срыв полоски.
/// </summary>
public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly InteractionSystem _interaction = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

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

        var user = args.User;

        if (!TryPeelTape(tape, user, out var doAfter))
            return;

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-peel-start"), user, user);
        args.Handled = true;
    }

    private void OnPeelTapeDoAfter(Entity<DuctTapeComponent> tape, ref Scp933PeelTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryCompletePeelTape(tape, args.User))
            return;
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

        if (!TryCompleteApplyTape(tapeMask, args.User, victim))
            return;
    }

    private void OnHumanoidInteractHand(Entity<HumanoidAppearanceComponent> target, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        var isHost = HasComp<Scp933MasterComponent>(args.User);
        var emergencyMode = false;

        if (!isHost)
        {
            // Без хоста разрешаем только один аварийный ритуал срыва на конкретной ленте.
            if (HasAnyScp933Host())
            {
                _popup.PopupEntity(Loc.GetString("scp933-rip-master-only"), args.User, args.User, PopupType.MediumCaution);
                return;
            }

            if (!TryGetScp933TapeMask(target.Owner, out var emergencyMaskUid) ||
                !TryComp<Scp933TapeMaskComponent>(emergencyMaskUid, out var emergencyMask) ||
                !emergencyMask.EmergencyRipAvailable)
            {
                _popup.PopupEntity(Loc.GetString("scp933-rip-master-only"), args.User, args.User, PopupType.MediumCaution);
                return;
            }

            emergencyMode = true;
        }

        if (!TryRipTape(args.User, target.Owner, emergencyMode, out var doAfter))
            return;

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-rip-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnTapeBeingUnequippedAttempt(Entity<Scp933TapeMaskComponent> tape, ref BeingUnequippedAttemptEvent args)
    {
        if (tape.Comp.RitualUnequipAllowed
            && tape.Comp.RitualUnequipUser == args.Unequipee
            && (HasComp<Scp933MasterComponent>(args.Unequipee) || tape.Comp.RitualAllowNonHost))
            return;

        // Лента снимается только через наш ритуал do-after и только хостом.
        args.Cancel();
    }

    private void OnTapeMaskGotEquipped(Entity<Scp933TapeMaskComponent> tapeMask, ref GotEquippedEvent args)
    {
        if (args.Slot != "mask")
            return;

        EnsureComp<MutedComponent>(args.Equipee);
    }

    private void OnTapeMaskGotUnequipped(Entity<Scp933TapeMaskComponent> tapeMask, ref GotUnequippedEvent args)
    {
        if (args.Slot != "mask")
            return;

        if (HasComp<Scp933FaceTornComponent>(args.Equipee))
            return;

        RemComp<MutedComponent>(args.Equipee);
    }

    private void OnRipTapeDoAfter(Entity<HumanoidAppearanceComponent> target, ref Scp933RipTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (!TryCompleteRipTape(args.User, target, args.ExpectedMask, args.EmergencyMode))
            return;
    }

    public bool TryPeelTape(Entity<DuctTapeComponent> tape, EntityUid user, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanPeelTape(tape, user))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            MathF.Max(0.1f, tape.Comp.PeelDelaySeconds),
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

    public bool TryCompletePeelTape(Entity<DuctTapeComponent> tape, EntityUid user)
    {
        if (!CanPeelTape(tape, user))
            return false;

        DoPeelTape(tape, user);
        return true;
    }

    private bool CanPeelTape(Entity<DuctTapeComponent> tape, EntityUid user)
    {
        return tape.Comp.UseCount > 0;
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

        tape.Comp.UseCount--;
        if (tape.Comp.UseCount <= 0)
            QueueDel(tape);
        else
            Dirty(tape);

        _audio.PlayPvs(tape.Comp.PullFromRollSound, user);
        _popup.PopupEntity(Loc.GetString("scp933-peel-success"), user, user);
    }

    public bool TryApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!CanApplyTape((tapeMask.Owner, tapeMask.Comp), user, victim))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            MathF.Max(0.1f, tapeMask.Comp.ApplyDelaySeconds),
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

    public bool TryCompleteApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
    {
        if (!CanApplyTape(tapeMask, user, victim))
            return false;

        DoApplyTape(tapeMask, user, victim);
        return true;
    }

    private bool CanApplyTape(Entity<Scp933TapeMaskComponent> tapeMask, EntityUid user, EntityUid victim)
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

        _audio.PlayPvs(tapeMask.Comp.ApplyToFaceSound, victim);
        _popup.PopupEntity(Loc.GetString("scp933-apply-success-user"), user, user);
        _popup.PopupEntity(Loc.GetString("scp933-apply-success-target"), victim, victim, PopupType.MediumCaution);
    }

    public bool TryRipTape(EntityUid user, EntityUid target, bool emergencyMode, out DoAfterArgs doAfter)
    {
        doAfter = default!;

        if (!TryGetScp933TapeMask(target, out var maskUid))
            return false;

        if (!TryComp<Scp933TapeMaskComponent>(maskUid, out var tapeMask))
            return false;

        if (!_interaction.InRangeUnobstructed(user, target, popup: true))
            return false;

        doAfter = new DoAfterArgs(EntityManager,
            user,
            MathF.Max(0.1f, tapeMask.RipDelaySeconds),
            new Scp933RipTapeDoAfterEvent
            {
                ExpectedMask = GetNetEntity(maskUid),
                EmergencyMode = emergencyMode
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

    public bool TryCompleteRipTape(EntityUid user, EntityUid target, NetEntity expectedMask, bool emergencyMode)
    {
        if (!CanRipTape(user, target, expectedMask, out var tapeMask))
            return false;

        DoRipTape(user, target, tapeMask, emergencyMode);
        return true;
    }

    private bool CanRipTape(EntityUid user, EntityUid target, NetEntity expectedMask, out Scp933TapeMaskComponent tapeMask)
    {
        tapeMask = default!;

        var expectedMaskUid = GetEntity(expectedMask);
        if (expectedMaskUid == null)
            return false;

        if (!TryGetScp933TapeMask(target, out var currentMask) || currentMask != expectedMaskUid)
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

        if (emergencyMode || !HasAnyScp933Host())
        {
            ConvertToMaster(target);
            ApplyHostBuffs(target);
        }
        else
        {
            ApplyFaceTornAfterRip(user, target);
        }

        _audio.PlayPvs(tapeMask.RipFromFaceSound, target);
        _popup.PopupEntity(Loc.GetString("scp933-rip-success-user"), user, user);
        _popup.PopupEntity(Loc.GetString("scp933-rip-success-target"), target, target, PopupType.MediumCaution);
    }

    private bool HasAnyScp933Host()
    {
        var query = EntityQueryEnumerator<Scp933MasterComponent>();
        return query.MoveNext(out _, out _);
    }

    private void ApplyHostBuffs(EntityUid uid)
    {
        if (!TryComp<MobThresholdsComponent>(uid, out var thresholds))
            return;

        _mobThresholds.SetMobStateThreshold(uid, FixedPoint2.Zero, MobState.Alive, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, 500, MobState.Critical, thresholds);
        _mobThresholds.SetMobStateThreshold(uid, 800, MobState.Dead, thresholds);
        _mobThresholds.VerifyThresholds(uid, thresholds);

        if (TryComp<MeleeWeaponComponent>(uid, out var melee))
        {
            melee.Damage = new DamageSpecifier { DamageDict = { ["Blunt"] = FixedPoint2.New(25) } };
            Dirty(uid, melee);
        }
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
