using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
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
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Localization;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Сервер: полный цикл ленты SCP-933 с do-after:
/// отрыв полоски от рулона -> наклеивание полоски -> срыв полоски.
/// </summary>
public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    /// <summary>Прототип оторванной полоски (не рулон).</summary>
    public const string TapeMaskPrototype = "ClothingMaskScp933Tape";
    private static readonly SoundSpecifier ApplyToFaceSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/ducttape.ogg");
    private static readonly SoundSpecifier RipFromFaceSound = new SoundPathSpecifier("/Audio/_Scp/Scp933/peeloff.ogg");

    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

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

        if (!TryComp<DuctTapeComponent>(tape, out var ductTape))
            return;

        if (ductTape.UseCount <= 0)
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            user,
            MathF.Max(0.1f, ductTape.PeelDelaySeconds),
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

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-peel-start"), user, user);
        args.Handled = true;
    }

    private void OnPeelTapeDoAfter(Entity<DuctTapeComponent> tape, ref Scp933PeelTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.User;
        if (!TryComp<DuctTapeComponent>(tape, out var ductTape))
            return;

        if (ductTape.UseCount <= 0)
            return;

        var peel = Spawn(TapeMaskPrototype);

        if (!_hands.TryPickupAnyHand(user, peel))
        {
            QueueDel(peel);
            _popup.PopupEntity(Loc.GetString("scp933-peel-hand-fail"), user, user, PopupType.MediumCaution);
            return;
        }

        ductTape.UseCount--;
        if (ductTape.UseCount <= 0)
            QueueDel(tape);
        else
            Dirty(tape, ductTape);

        _audio.PlayPvs(ductTape.PullFromRollSound, user);
        _popup.PopupEntity(Loc.GetString("scp933-peel-success"), user, user);
    }

    private void OnHumanoidInteractUsing(Entity<HumanoidAppearanceComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<Scp933TapeMaskComponent>(args.Used, out var tapeMask))
            return;

        if (TryGetScp933TapeMask(target.Owner, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already"), args.User, args.User);
            return;
        }

        if (!_interaction.InRangeUnobstructed(args.User, target.Owner, popup: true))
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            MathF.Max(0.1f, tapeMask.ApplyDelaySeconds),
            new Scp933ApplyTapeDoAfterEvent(),
            args.Used,
            target: target.Owner,
            used: args.Used)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
            NeedHand = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("scp933-apply-start"), args.User, args.User);
        args.Handled = true;
    }

    private void OnApplyTapeDoAfter(Entity<Scp933TapeMaskComponent> tapeMask, ref Scp933ApplyTapeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        var user = args.User;
        if (args.Target is not { } victim)
            return;

        if (!TryComp<Scp933TapeMaskComponent>(tapeMask, out _))
            return;

        if (!TryComp<HumanoidAppearanceComponent>(victim, out _))
            return;

        if (!_interaction.InRangeUnobstructed(user, victim, popup: true))
            return;

        if (TryGetScp933TapeMask(victim, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already"), user, user);
            return;
        }

        if (_inventory.TryGetSlotEntity(victim, "mask", out _))
            _inventory.TryUnequip(user, victim, "mask", silent: true, force: true);

        if (!_inventory.TryEquip(user, victim, tapeMask, "mask", silent: true, force: true))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-equip-fail"), user, user, PopupType.MediumCaution);
            return;
        }

        _audio.PlayPvs(ApplyToFaceSound, victim);
        _popup.PopupEntity(Loc.GetString("scp933-apply-success-user"), user, user);
        _popup.PopupEntity(Loc.GetString("scp933-apply-success-target"), victim, victim, PopupType.MediumCaution);
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

        if (!TryGetScp933TapeMask(target.Owner, out var maskUid))
            return;

        if (!TryComp<Scp933TapeMaskComponent>(maskUid, out var tapeMask))
            return;

        if (!_interaction.InRangeUnobstructed(args.User, target.Owner, popup: true))
            return;

        var doAfter = new DoAfterArgs(EntityManager,
            args.User,
            MathF.Max(0.1f, tapeMask.RipDelaySeconds),
            new Scp933RipTapeDoAfterEvent
            {
                ExpectedMask = GetNetEntity(maskUid),
                EmergencyMode = emergencyMode
            },
            target.Owner,
            target: target.Owner)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

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

        var user = args.User;

        var expectedMask = GetEntity(args.ExpectedMask);
        if (expectedMask == null)
            return;

        if (!TryGetScp933TapeMask(target.Owner, out var currentMask) || currentMask != expectedMask)
            return;

        if (!TryComp<Scp933TapeMaskComponent>(currentMask, out var tapeMask))
            return;

        tapeMask.RitualUnequipAllowed = true;
        tapeMask.RitualUnequipUser = user;
        tapeMask.RitualAllowNonHost = args.EmergencyMode;
        var unequipped = _inventory.TryUnequip(user, target, "mask", out var removed, silent: true, force: true);
        tapeMask.RitualUnequipAllowed = false;
        tapeMask.RitualUnequipUser = null;
        tapeMask.RitualAllowNonHost = false;

        if (!unequipped)
            return;

        if (removed != null)
            QueueDel(removed.Value);

        if (args.EmergencyMode)
            tapeMask.EmergencyRipAvailable = false;

        if (args.EmergencyMode || !HasAnyScp933Host())
        {
            ConvertToMaster(target);
            ApplyHostBuffs(target);
        }
        else
        {
            ApplyFaceTornAfterRip(user, target);
        }

        _audio.PlayPvs(RipFromFaceSound, target);
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
