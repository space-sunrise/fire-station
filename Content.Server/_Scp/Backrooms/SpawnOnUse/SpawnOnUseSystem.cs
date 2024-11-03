using Content.Server.Administration.Logs;
using Content.Server.Popups;
using Content.Shared.Database;
using Content.Shared.Interaction.Events;
using Content.Shared.Timing;
using Robust.Server.Audio;

namespace Content.Server._Scp.Backrooms.SpawnOnUse;

public sealed class SpawnOnUseSystem : EntitySystem
{
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SpawnOnUseComponent, UseInHandEvent>(OnUse);
    }

    private void OnUse(Entity<SpawnOnUseComponent> item, ref UseInHandEvent args)
    {
        if (item.Comp.Charges <= 0)
        {
            if (item.Comp.PopupNoCharges != null)
                _popup.PopupEntity(Loc.GetString(item.Comp.PopupNoCharges), item, args.User);

            return;
        }

        if (!TryComp<UseDelayComponent>(item, out var delay) || _useDelay.IsDelayed((item, delay)))
            return;

        foreach (var entity in item.Comp.Entities)
        {
            if (item.Comp.Charges <= 0)
                continue;

            var ent = Spawn(entity, Transform(args.User).Coordinates);
            item.Comp.Charges -= 1;

            _adminLogger.Add(LogType.EntitySpawn, LogImpact.Medium,
                $"{ToPrettyString(args.User):user} spawned {ToPrettyString(ent):entity} via {ToPrettyString(item):item}");
        }

        if (item.Comp.SoundSuccessFul != null)
            _audio.PlayPvs(item.Comp.SoundSuccessFul, item);

        _useDelay.TryResetDelay(item);
    }
}
