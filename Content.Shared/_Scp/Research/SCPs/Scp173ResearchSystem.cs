using Content.Shared._Scp.Mobs.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Research.SCPs;

public sealed partial class Scp173ResearchSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist= default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpRestrictionComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<ScpRestrictionComponent, ScpSpawnInteractDoAfterEvent>(OnInteractSuccessful);
    }

    private void OnInteract(Entity<ScpRestrictionComponent> scp, ref InteractUsingEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        var item = args.Used;

        if (!TryComp<ScpResearchToolComponent>(item, out var researchTool))
            return;

        if (!_whitelist.IsWhitelistPass(researchTool.Whitelist, scp))
            return;

        var timeLeft = _timing.CurTime - scp.Comp.TimeLastUsed;

        if (scp.Comp.TimeLastUsed != null && timeLeft < researchTool.Cooldown)
        {
            // TODO: Более красивое форматирование времени

            if (researchTool.CooldownMessage != null)
            {
                var message = Loc.GetString(researchTool.CooldownMessage, ("time", researchTool.Cooldown - timeLeft));
                _popup.PopupClient(message, args.User, args.User);
            }

            return;
        }

        var ev = researchTool.Event;
        ev.Tool = (item, researchTool);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, args.User, researchTool.Delay, ev, scp, target: scp, used: item)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = true,
        };

        _doAfterSystem.TryStartDoAfter(doAfterEventArgs);
    }

    private void OnInteractSuccessful(Entity<ScpRestrictionComponent> scp, ref ScpSpawnInteractDoAfterEvent args)
    {
        if (!_timing.IsFirstTimePredicted)
            return;

        if (args.Cancelled || args.Handled)
            return;

        var tool = args.Tool;

        if (tool.Comp.Sound != null)
            _audio.PlayEntity(tool.Comp.Sound, scp, scp);

        if (_net.IsServer)
            Spawn(args.ToSpawn, Transform(scp).Coordinates);

        // Задаем последнее время использования для кулдауна
        scp.Comp.TimeLastUsed = _timing.CurTime;
        Dirty(scp);

        args.Handled = true;
    }

}
