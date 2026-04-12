using System.Linq;
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
        ent.Comp.SortedBloodRequiredPerState = ent.Comp.BloodRequiredPerState
            .OrderBy(kv => kv.Value)
            .ToList();

        if (!_fearQuery.TryComp(ent, out var fearComponent))
        {
            Log.Warning($"Found entity {ToPrettyString(ent)} with {nameof(HemophobiaComponent)} but without {nameof(FearComponent)}! {nameof(HemophobiaComponent)} will be deleted");
            RemComp<HemophobiaComponent>(ent);

            return;
        }

        fearComponent.Phobias.Add(ent.Comp.Phobia);
        DirtyField(ent, fearComponent, nameof(FearComponent.Phobias));
    }

    /// <summary>
    /// Вызывается при завершении работы компонента гемофобии.
    /// Убирает фобию крови из списка фобий персонажа.
    /// </summary>
    private void OnHemophobiaShutdown(Entity<HemophobiaComponent> ent, ref ComponentShutdown args)
    {
        if (!_fearQuery.TryComp(ent, out var fearComponent))
            return;

        fearComponent.Phobias.Remove(ent.Comp.Phobia);
        DirtyField(ent, fearComponent, nameof(FearComponent.Phobias));
    }
}
