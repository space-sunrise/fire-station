using Content.Server.Administration.Systems;
using Content.Shared._Scp.Fear;
using Content.Shared._Scp.Fear.Components.Traits;
using Content.Shared.Administration;
using Robust.Shared.Random;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    [Dependency] private readonly AdminFrozenSystem _adminFrozen = default!;

    private void InitializeTraits()
    {
        SubscribeLocalEvent<FearStuporComponent, FearStateChangedEvent>(OnStuporFearStateChanged);
    }

    /// <summary>
    /// Обрабатывает событие изменения уровня страха.
    /// С шансом вызывает оцепенение у персонажа с трейтом оцепенения.
    /// </summary>
    private void OnStuporFearStateChanged(Entity<FearStuporComponent> ent, ref FearStateChangedEvent args)
    {
        // Если старый стейт больше, значит персонаж успокоился
        // От этого не нужно впадать в ступор
        if (args.OldState > args.NewState)
            return;

        if (args.NewState < ent.Comp.RequiredState)
            return;

        var normalizedChance = PercentToNormalized(ent.Comp.Chance);
        if (!_random.Prob(normalizedChance))
            return;

        AddStupor(ent, ent.Comp.StuporTime);
    }

    /// <summary>
    /// Добавляет эффект оцепенения на персонажа на указанное время.
    /// </summary>
    private void AddStupor(EntityUid uid, TimeSpan time)
    {
        _adminFrozen.FreezeAndMute(uid);

        RemoveComponentAfter<AdminFrozenComponent>(uid, time);
    }
}
