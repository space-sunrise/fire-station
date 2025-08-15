using Content.Server.StationRecords.Components;
using Content.Shared._Sunrise.StationRecords;
using Content.Shared.StationRecords;
using Robust.Shared.Prototypes;

namespace Content.Server.StationRecords.Systems;

public sealed partial class GeneralStationRecordConsoleSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private void InitializeSunrise()
    {
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
        _stationRecords.RemoveRecord(new StationRecordKey(args.Id, owning.Value));

        // Добавляем новую
        var id = _stationRecords.AddRecordEntry(owning.Value, GeneralStationRecord.SanitizeRecord(args.Record, in _prototype));
        ent.Comp.ActiveKey = id.Id;

        // TODO: Радио оповещение в канал Командования/СБ
        // TODO: Крутой пикающий звук
        UpdateUserInterface(ent);
    }
}
