using Content.Server.Popups;
using Content.Server.Radio.EntitySystems;
using Content.Server.StationRecords.Components;
using Content.Shared._Sunrise.StationRecords;
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
        if (ent.Comp.CanRedactSensitiveData && ent.Comp.CanDeleteEntries)
            return;

        if (args.Handled)
            return;

        ent.Comp.CanDeleteEntries = true;
        ent.Comp.CanRedactSensitiveData = true;

        UpdateUserInterface(ent);
        args.Handled = true;
    }

    private void DoFeedback(Entity<GeneralStationRecordConsoleComponent> ent, string message, string popup)
    {
        foreach (var channel in ent.Comp.AnnouncementChannels)
        {
            _radio.SendRadioMessage(ent, message, channel, ent);
        }

        _audio.PlayPvs(ent.Comp.SuccessfulSound, ent);
        _popup.PopupEntity(popup, ent);
    }
}
