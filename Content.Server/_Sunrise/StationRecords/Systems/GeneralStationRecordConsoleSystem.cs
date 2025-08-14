using Content.Server.StationRecords.Components;
using Content.Shared._Sunrise.StationRecords;
using Content.Shared.StationRecords;

namespace Content.Server.StationRecords.Systems;

public sealed partial class GeneralStationRecordConsoleSystem
{
    private void InitializeSunrise()
    {
        Subs.BuiEvents<GeneralStationRecordConsoleComponent>(GeneralStationRecordConsoleKey.Key, subs =>
        {
            subs.Event<SaveStationRecord>(OnSave);
        });
    }

    private void OnSave(Entity<GeneralStationRecordConsoleComponent> ent, ref SaveStationRecord args)
    {
        // TODO: Санитизация инпута от игрока

        var owning = _station.GetOwningStation(ent.Owner);

        if (owning == null)
            return;

        // Удаляем старую запись
        _stationRecords.RemoveRecord(new StationRecordKey(args.Id, owning.Value));

        // Добавляем новую
        var id = _stationRecords.AddRecordEntry(owning.Value, args.Record);
        ent.Comp.ActiveKey = id.Id;

        // TODO: Радио оповещение в канал Командования/СБ
        // TODO: Крутой пикающий звук
        UpdateUserInterface(ent);
    }
}
