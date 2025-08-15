using Content.Server.StationRecords.Components;
using Content.Shared._Sunrise.StationRecords;
using Content.Shared.Emag.Systems;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.StationRecords.Systems;

public sealed partial class GeneralStationRecordConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

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

        // TODO: Радио оповещение в канал Командования/СБ
        // TODO: Крутой пикающий звук
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
}
