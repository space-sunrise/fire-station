using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.StationRecords.Components;
using Content.Shared._Sunrise.StationRecords;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.StationRecords;
using Robust.Server.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.StationRecords.Systems;

public sealed partial class GeneralStationRecordConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AccessReaderSystem _access = default!;

    private void InitializeSunrise()
    {
        SubscribeLocalEvent<GeneralStationRecordConsoleComponent, GotEmaggedEvent>(OnEmagged);

        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key, subs =>
        {
            subs.Event<SaveStationRecord>(OnSave);
        });
    }

    private void OnSave(Entity<GeneralStationRecordConsoleComponent> ent, ref SaveStationRecord args)
    {
        var owning = _station.GetOwningStation(ent.Owner);

        if (owning == null)
            return;

        // Дополнительная серверная проверка на случай педиков с читами
        if (!HasAccess(ent, args.Actor))
            return;

        // Удаляем старую запись
        if (!_stationRecords.RemoveRecord(new StationRecordKey(args.Id, owning.Value)))
            return;

        // Добавляем новую
        var record = GeneralStationRecord.SanitizeRecord(args.Record, in _prototype);
        var id = _stationRecords.AddRecordEntry(owning.Value, record);
        ent.Comp.ActiveKey = id.Id;

        var message = Loc.GetString("station-record-updated", ("name", args.Record.Name));
        var popup = Loc.GetString("station-record-updated-successfully");

        DoFeedback(ent, message, popup);

        UpdateUserInterface(ent);
    }

    private void OnEmagged(Entity<GeneralStationRecordConsoleComponent> ent, ref GotEmaggedEvent args)
    {
        if (ent.Comp.CanRedactSensitiveData
            && ent.Comp.CanDeleteEntries
            && ent.Comp.Silent
            && ent.Comp.SkipAccessCheck)
            return;

        if (args.Handled)
            return;

        ent.Comp.CanDeleteEntries = true;
        ent.Comp.CanRedactSensitiveData = true;
        ent.Comp.Silent = true;
        ent.Comp.SkipAccessCheck = true;

        UpdateUserInterface(ent);
        args.Handled = true;
    }

    private void DoFeedback(Entity<GeneralStationRecordConsoleComponent> ent, string message, string popup)
    {
        _popup.PopupEntity(popup, ent);

        if (ent.Comp.Silent)
            return;

        foreach (var channel in ent.Comp.AnnouncementChannels)
        {
            _radio.SendRadioMessage(ent, message, channel, ent);
        }

        _audio.PlayPvs(ent.Comp.SuccessfulSound, ent);
    }

    private void OnOpened(Entity<GeneralStationRecordConsoleComponent> ent, ref BoundUIOpenedEvent msg)
    {
        ent.Comp.HasAccess = HasAccess(ent, msg.Actor);
        UpdateUserInterface(ent);
    }

    /// <summary>
    /// Проверяет наличие у персонажа доступа к консоли.
    /// </summary>
    private bool HasAccess(Entity<GeneralStationRecordConsoleComponent> ent, EntityUid actor)
    {
        var allowed = _access.IsAllowed(actor, ent);
        return allowed || ent.Comp.SkipAccessCheck;
    }
}
