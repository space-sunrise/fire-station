using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Shaders;
using Content.Shared._Scp.Shaders.Grain;
using Content.Shared._Scp.Shaders.Vignette;
using Content.Shared._Scp.Watching;
using Robust.Shared.Timing;

namespace Content.Shared._Scp.Fear;

public sealed partial class FearSystem : EntitySystem
{
    [Dependency] private readonly SharedShaderStrengthSystem _shaderStrength = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, EntityLookedAtEvent>(OnEntityLookedAt);

        SubscribeLocalEvent<FearComponent, ProximityInRangeTargetEvent>(OnProximityInRange);
        SubscribeLocalEvent<FearComponent, ProximityNotInRangeTargetEvent>(OnProximityNotInRange);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<FearComponent>();

        // Проходимся по людям с компонентом страха и уменьшаем уровень страха со временем
        while (query.MoveNext(out var uid, out var fear))
        {
            if (fear.State == FearState.None)
                continue;

            if (fear.NextTimeDecreaseFearLevel > _timing.CurTime)
                continue;

            var newFearState = GetDecreasedLevel(fear.State);

            if (!TrySetFearLevel((uid, fear), newFearState))
                continue;

            // TODO: Звук облегчения
        }
    }

    private void OnEntityLookedAt(Entity<FearComponent> ent, ref EntityLookedAtEvent args)
    {
        if (!args.Target.Comp.AlreadyLookedAt.TryGetValue(GetNetEntity(ent), out var lastSeenTime))
            return;

        if (_timing.CurTime < lastSeenTime + ent.Comp.TimeToGetScaredAgainOnLookAt)
            return;

        if (!TryComp<FearSourceComponent>(args.Target, out var fearSource))
            return;

        if (!TrySetFearLevel(ent.AsNullable(), fearSource.UponSeenState))
            return;
    }

    private void OnProximityInRange(Entity<FearComponent> ent, ref ProximityInRangeTargetEvent args)
    {
        Logger.Error($"{Name(ent)}, {Name(args.Receiver)}, {args.Range}, {args.Type}");

        if (!TryComp<FearSourceComponent>(args.Receiver, out var source))
            return;

        TrySetFearLevel(ent.AsNullable(), source.UponComeCloser);

        SetRangeBasedShaderStrength<GrainOverlayComponent>(ent.Owner, args.Range, args.CloseRange, source.GrainShaderStrength);
        SetRangeBasedShaderStrength<VignetteOverlayComponent>(ent.Owner, args.Range, args.CloseRange, source.VignetteShaderStrength);
    }

    private void OnProximityNotInRange(Entity<FearComponent> ent, ref ProximityNotInRangeTargetEvent args)
    {
        Logger.Debug($"{Name(ent)}");

        // Как только игрок отходит от источника страха он должен перестать бояться
        // Но значения шейдера от уровня страха должны продолжать действовать, что и учитывает метод
        SetShaderStrength<GrainOverlayComponent>(ent.Owner, ent.Comp, 0f);
        SetShaderStrength<VignetteOverlayComponent>(ent.Owner, ent.Comp, 0f);
    }

    /// <summary>
    /// Пытается установить переданный уровень страха.
    /// </summary>
    public bool TrySetFearLevel(Entity<FearComponent?> ent, FearState state)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State == state)
            return true;

        ent.Comp.State = state;

        SetFearBasedShaderStrength((ent, ent.Comp));
        SetNextCalmDownTime((ent, ent.Comp));

        Dirty(ent);

        return true;
    }

    /// <summary>
    /// Устанавливает силу шейдеров в зависимости от текущего уровня страха.
    /// </summary>
    private void SetFearBasedShaderStrength(Entity<FearComponent> ent)
    {
        var grainStrength = ent.Comp.FearBasedGrainStrength[ent.Comp.State];
        var vignetteStrength = ent.Comp.FearBasedVignetteStrength[ent.Comp.State];

        _shaderStrength.TrySetAdditionalStrength<GrainOverlayComponent>(ent.Owner, grainStrength);
        _shaderStrength.TrySetAdditionalStrength<VignetteOverlayComponent>(ent.Owner, vignetteStrength);

        ent.Comp.CurrentFearBasedShaderStrength[nameof(GrainOverlayComponent)] = grainStrength;
        ent.Comp.CurrentFearBasedShaderStrength[nameof(VignetteOverlayComponent)] = vignetteStrength;
    }

    /// <summary>
    /// Устанавливает для шейдера параметры силы.
    /// Сила зависит от расстояния до источника страха и параметров самого источника.
    /// </summary>
    /// <param name="ent">Сущность, с компонентами силы шейдера</param>
    /// <param name="currentRange">Текущее расстояние сущности до источника страха</param>
    /// <param name="maxRange">Расстояние, на котором начинаются увеличиваться параметры шейдера</param>
    /// <param name="parameters">Параметры силы шейдера. Минимальное означает силу шейдера на входе в <see cref="maxRange"/>,
    /// максимальное силу при максимальном приближении к источнику страха</param>
    /// <param name="fear">Компонент страха</param>
    /// <typeparam name="T">Компонент с настройками шейдера</typeparam>
    private void SetRangeBasedShaderStrength<T>(Entity<T?> ent, float currentRange, float maxRange, MinMaxExtended parameters, FearComponent? fear = null)
        where T : IShaderStrength, IComponent
    {
        if (!Resolve(ent, ref fear))
            return;

        var strength = CalculateShaderStrength(currentRange, maxRange, parameters);
        SetShaderStrength(ent, fear, strength);
    }

    /// <summary>
    /// Устанавливает силу шейдера, учитывая текущий уровень страха и его потребности в силе.
    /// </summary>
    /// <typeparam name="T">Компонент настроек, контролирующий силу шейдера</typeparam>
    public void SetShaderStrength<T>(Entity<T?> ent, FearComponent? fear, float strength)
        where T : IShaderStrength, IComponent
    {
        if (!Resolve(ent, ref fear))
            return;

        var actualStrength = GetActualStrength<T>(fear, strength);
        _shaderStrength.TrySetAdditionalStrength(ent, actualStrength);
    }
}
