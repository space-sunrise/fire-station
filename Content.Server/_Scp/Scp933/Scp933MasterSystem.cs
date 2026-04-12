using Content.Shared._Scp.Scp933;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
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
using Content.Shared.Standing;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Localization;
using Robust.Shared.Network;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Сервер: отрыв полоски с рулона (спавн ClothingMaskScp933Tape — только визуал полоски, рулон остаётся с UseCount),
/// самонаклеивание, инкубация, хост наклеивает жертве (InteractUsing), срыв (InteractHand), стабилизация уложенной жертвы.
/// </summary>
public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    /// <summary>Прототип оторванной полоски (не рулон).</summary>
    public const string TapeMaskPrototype = "ClothingMaskScp933Tape";

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DuctTapeComponent, UseInHandEvent>(OnDuctTapeUseInHand);
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractUsingEvent>(OnHumanoidInteractUsing);
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractHandEvent>(OnHumanoidInteractHand);
        SubscribeLocalEvent<Scp933TapeMaskComponent, GotEquippedEvent>(OnTapeMaskGotEquipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<Scp933PendingHostComponent>();
        while (query.MoveNext(out var uid, out var pending))
        {
            pending.RemainingSeconds -= frameTime;
            if (pending.RemainingSeconds > 0f)
                continue;

            RemComp<Scp933PendingHostComponent>(uid);

            if (_inventory.TryGetSlotEntity(uid, "mask", out var maskEntity) &&
                HasComp<Scp933TapeMaskComponent>(maskEntity))
            {
                _inventory.TryUnequip(uid, "mask", out _, silent: true, force: true);
                QueueDel(maskEntity);
            }

            ConvertToMaster(uid);
            ApplyHostBuffs(uid);
        }
    }

    private void OnTapeMaskGotEquipped(Entity<Scp933TapeMaskComponent> mask, ref GotEquippedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (!mask.Comp.AwaitingHostTransformation)
            return;

        mask.Comp.AwaitingHostTransformation = false;
        Dirty(mask);

        var pending = EnsureComp<Scp933PendingHostComponent>(args.Equipee);
        pending.RemainingSeconds = MathF.Max(1f, mask.Comp.IncubationSeconds);
    }

    private void OnDuctTapeUseInHand(Entity<DuctTapeComponent> tape, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        var user = args.User;

        if (!TryComp<HumanoidAppearanceComponent>(user, out _))
            return;

        if (HasComp<Scp933MasterComponent>(user) || HasComp<Scp933PendingHostComponent>(user))
            return;

        if (TryGetScp933TapeMask(user, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-self-already-mask"), user, user);
            return;
        }

        if (TrySelfApplyDuctTape(user, tape))
            args.Handled = true;
    }

    private bool TrySelfApplyDuctTape(EntityUid user, Entity<DuctTapeComponent> tape)
    {
        if (!TryComp<DuctTapeComponent>(tape, out var duct))
            return false;

        var mask = Spawn(TapeMaskPrototype);
        if (!TryComp<Scp933TapeMaskComponent>(mask, out var tapeMask))
        {
            QueueDel(mask);
            return false;
        }

        tapeMask.AwaitingHostTransformation = true;
        Dirty(mask, tapeMask);

        if (_inventory.TryGetSlotEntity(user, "mask", out _))
            _inventory.TryUnequip(user, "mask", silent: true, force: true);

        if (!_inventory.TryEquip(user, mask, "mask", silent: true, force: true))
        {
            _popup.PopupEntity(Loc.GetString("scp933-self-equip-fail"), user, user);
            QueueDel(mask);
            return false;
        }

        duct.UseCount--;
        if (duct.UseCount <= 0)
            QueueDel(tape);
        else
            Dirty(tape, duct);

        _popup.PopupEntity(Loc.GetString("scp933-self-taped"), user, user, PopupType.MediumCaution);
        return true;
    }

    private void OnHumanoidInteractUsing(Entity<HumanoidAppearanceComponent> target, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<DuctTapeComponent>(args.Used))
            return;

        if (!HasComp<Scp933MasterComponent>(args.User))
            return;

        if (args.User == target.Owner)
            return;

        if (TryApplyTapePhase(args.User, target.Owner, args.Used))
            args.Handled = true;
    }

    private void OnHumanoidInteractHand(Entity<HumanoidAppearanceComponent> target, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<Scp933MasterComponent>(args.User))
            return;

        if (args.User == target.Owner)
            return;

        if (!TryGetScp933TapeMask(target.Owner, out _))
            return;

        if (TryRipTapePhase(args.User, target.Owner))
            args.Handled = true;
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

    private bool TryApplyTapePhase(EntityUid master, EntityUid victim, EntityUid tape)
    {
        if (!TryComp<HumanoidAppearanceComponent>(victim, out _))
            return false;

        if (!HasComp<Scp933MasterComponent>(master))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-master-only"), master, master);
            return false;
        }

        if (TryGetScp933TapeMask(victim, out _))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already"), master, master);
            return false;
        }

        if (HasComp<Scp933FaceTornComponent>(victim))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already-faceless"), master, master);
            return false;
        }

        if (HasComp<Scp933MasterComponent>(victim))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-no-other-master"), master, master);
            return false;
        }

        if (!_interaction.InRangeUnobstructed(master, victim, popup: true))
            return false;

        if (!TryComp<DuctTapeComponent>(tape, out var duct))
            return false;

        var mask = Spawn(TapeMaskPrototype);
        if (!TryComp<Scp933TapeMaskComponent>(mask, out var tapeMask))
        {
            QueueDel(mask);
            return false;
        }

        tapeMask.AwaitingHostTransformation = false;
        Dirty(mask, tapeMask);

        if (_inventory.TryGetSlotEntity(victim, "mask", out _))
            _inventory.TryUnequip(master, victim, "mask", silent: true, force: true);

        if (!_inventory.TryEquip(master, victim, mask, "mask", silent: true, force: true))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-equip-fail"), master, master);
            QueueDel(mask);
            return false;
        }

        duct.UseCount--;
        if (duct.UseCount <= 0)
            QueueDel(tape);
        else
            Dirty(tape, duct);

        _popup.PopupEntity(Loc.GetString("scp933-tape-applied-master"), master, master);
        _popup.PopupEntity(Loc.GetString("scp933-tape-applied-victim"), victim, victim, PopupType.MediumCaution);

        TryStabilizeTapedVictim(victim);

        return true;
    }

    private bool TryRipTapePhase(EntityUid master, EntityUid victim)
    {
        if (!TryGetScp933TapeMask(victim, out _))
            return false;

        if (!HasComp<Scp933MasterComponent>(master))
            return false;

        if (!_interaction.InRangeUnobstructed(master, victim, popup: true))
            return false;

        if (HasComp<Scp933MasterComponent>(victim))
            return false;

        if (!_inventory.TryUnequip(master, victim, "mask", out var removed, silent: true, force: true))
            return false;

        if (removed != null)
            QueueDel(removed.Value);

        ApplyFaceTornAfterRip(master, victim);

        TryStabilizeTapedVictim(victim);

        _popup.PopupEntity(Loc.GetString("scp933-tape-ripped-master"), master, master);

        return true;
    }

    /// <summary>
    /// После укладки жертва часто в крите/лежит — сбрасываем урон и поднимаем, чтобы сцена не умерла до срыва.
    /// </summary>
    private void TryStabilizeTapedVictim(EntityUid victim)
    {
        if (TryComp<MobStateComponent>(victim, out var mob) && mob.CurrentState == MobState.Dead)
            return;

        if (!TryComp<DamageableComponent>(victim, out var dmg))
            return;

        if (dmg.TotalDamage > FixedPoint2.Zero)
        {
            _damageable.ClearAllDamage((victim, dmg));
            _popup.PopupEntity(Loc.GetString("scp933-victim-stabilized"), victim, victim, PopupType.Small);
        }

        _standing.Stand(victim, force: true);
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
}
