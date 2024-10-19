using Content.Shared._Scp.Scp173;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Research.SCPs;

public sealed partial class Scp173ResearchSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private ProtoId<TagPrototype> _pickaxeTag = "Pickaxe";
    private EntProtoId _scp173ShardId = "Crowbar"; // TODO: Сделать прототип шарда и поменять айди

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp173Component, InteractUsingEvent>(OnPickaxeInteract);
        SubscribeLocalEvent<Scp173Component, Scp173PickaxeInteractDoAfterEvent>(OnPickaxeInteractSuccessful);
    }

    #region Pickaxe

    private void OnPickaxeInteract(Entity<Scp173Component> scp, ref InteractUsingEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var pickaxe = args.Used;

        if (!_tag.HasTag(pickaxe, _pickaxeTag))
            return;

        var timeLeft = _timing.CurTime - scp.Comp.ChipOffLastUsed;

        // Кулдаун на откалывание
        if (scp.Comp.ChipOffLastUsed != null && timeLeft < scp.Comp.ChipOffCooldown)
        {
            // TODO: Более красивое форматирование времени
            _popup.PopupClient($"Следующее откалывание возможно через {scp.Comp.ChipOffCooldown - timeLeft}", args.User, args.User);

            return;
        }

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, scp.Comp.ChipOffDelay, new Scp173PickaxeInteractDoAfterEvent(), scp, target: scp, used: pickaxe)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnPickaxeInteractSuccessful(Entity<Scp173Component> scp, ref Scp173PickaxeInteractDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled || args.Handled)
            return;

        _audio.PlayEntity(scp.Comp.ChipOffSound, scp, scp);

        if (_net.IsServer)
            Spawn(_scp173ShardId, Transform(scp).Coordinates);

        // Задаем последнее время использования для кулдауна
        scp.Comp.ChipOffLastUsed = _timing.CurTime;
        Dirty(scp);

        args.Handled = true;
    }

    #endregion


    #region Events

    [Serializable, NetSerializable]
    public sealed partial class Scp173PickaxeInteractDoAfterEvent : SimpleDoAfterEvent
    {
    }

    #endregion

}
