using Content.Shared._Scp.Fear.Components;
using Content.Shared._Scp.Fear.Components.Fears;

namespace Content.Shared._Scp.Fear.Systems;

public abstract partial class SharedFearSystem
{
    private void InitializeFears()
    {
        SubscribeLocalEvent<HemophobiaComponent, ComponentStartup>(OnHemophobiaInit);
        SubscribeLocalEvent<HemophobiaComponent, ComponentShutdown>(OnHemophobiaShutdown);
    }

    /// <summary>
    /// Вызывается при старте компонента гемофобии.
    /// Добавляет айди фобии крови в список фобий персонажа.
    /// </summary>
    private void OnHemophobiaInit(Entity<HemophobiaComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<FearComponent>(ent, out var fearComponent))
            return;

        fearComponent.Phobias.Add(ent.Comp.Phobia);
        Dirty(ent, fearComponent);
    }

    /// <summary>
    /// Вызывается при завершении работы компонента гемофобии.
    /// Убирает фобию крови из списка фобий персонажа.
    /// </summary>
    private void OnHemophobiaShutdown(Entity<HemophobiaComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp<FearComponent>(ent, out var fearComponent))
            return;

        fearComponent.Phobias.Remove(ent.Comp.Phobia);
        Dirty(ent, fearComponent);
    }
}
