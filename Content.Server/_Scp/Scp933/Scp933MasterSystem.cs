using Content.Shared._Scp.Scp933;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Localization;

namespace Content.Server._Scp.Scp933;

/// <summary>
/// Сервер: ритуал SCP-933 — лента на лицо (InteractUsing), затем срыв пустой рукой (InteractHand).
/// </summary>
public sealed class Scp933MasterSystem : SharedScp933MasterSystem
{
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        // InteractUsing поднимается на цели (гуманоиде), а не на предмете в руке.
        SubscribeLocalEvent<HumanoidAppearanceComponent, InteractUsingEvent>(OnHumanoidInteractUsing);
        SubscribeLocalEvent<TapedFaceComponent, InteractHandEvent>(OnTapedFaceInteractHand);
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

    private void OnTapedFaceInteractHand(Entity<TapedFaceComponent> target, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<Scp933MasterComponent>(args.User))
            return;

        if (args.User == target.Owner)
            return;

        if (TryRipTapePhase(args.User, target.Owner))
            args.Handled = true;
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

        if (HasComp<TapedFaceComponent>(victim))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already"), master, master);
            return false;
        }

        if (HasComp<Scp933ControlledComponent>(victim))
        {
            _popup.PopupEntity(Loc.GetString("scp933-tape-already-slave"), master, master);
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

        AddComp(victim, new TapedFaceComponent());
        duct.UseCount--;

        if (duct.UseCount <= 0)
            QueueDel(tape);
        else
            Dirty(tape, duct);

        _popup.PopupEntity(Loc.GetString("scp933-tape-applied-master"), master, master);
        _popup.PopupEntity(Loc.GetString("scp933-tape-applied-victim"), victim, victim, PopupType.MediumCaution);

        return true;
    }

    private bool TryRipTapePhase(EntityUid master, EntityUid victim)
    {
        if (!HasComp<TapedFaceComponent>(victim))
            return false;

        if (!TryComp<Scp933MasterComponent>(master, out var masterComp))
            return false;

        if (!_interaction.InRangeUnobstructed(master, victim, popup: true))
            return false;

        if (HasComp<Scp933MasterComponent>(victim))
            return false;

        if (masterComp.Controlled.Count >= masterComp.MaxControlled)
        {
            _popup.PopupEntity(Loc.GetString("scp933-dominate-cap"), master, master);
            return false;
        }

        RemComp<TapedFaceComponent>(victim);
        DominateVictim(master, victim);

        _popup.PopupEntity(Loc.GetString("scp933-tape-ripped-master"), master, master);

        return true;
    }
}
