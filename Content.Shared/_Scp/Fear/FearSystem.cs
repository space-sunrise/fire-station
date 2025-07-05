using Content.Shared._Scp.Helpers;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Shaders;
using Content.Shared._Scp.Shaders.Grain;
using Content.Shared._Scp.Shaders.Vignette;

namespace Content.Shared._Scp.Fear;

public sealed class FearSystem : EntitySystem
{
    [Dependency] private readonly SharedShaderStrengthSystem _shaderStrength = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FearComponent, ProximityInRangeTargetEvent>(OnProximityInRange);
    }

    private void OnProximityInRange(Entity<FearComponent> ent, ref ProximityInRangeTargetEvent args)
    {
        Logger.Error($"{Name(ent)}, {Name(args.Receiver)}, {args.Range}, {args.Type}");

        if (!TryComp<FearSourceComponent>(args.Receiver, out var source))
            return;

        // TODO: Какой-то более надежный способ понять, что игрок отошел от источника страха
        // чем надеяться, что ProximitySystem успеет зарегать расстояние на границе и вызвать ивент

        SetShaderStrength<GrainOverlayComponent>(ent.Owner, args.Range, args.CloseRange, source.GrainShaderStrength);
        SetShaderStrength<VignetteOverlayComponent>(ent.Owner, args.Range, args.CloseRange, source.VignetteShaderStrength);

        TrySetFearLevel(ent.AsNullable(), source.UponComeCloser);
    }

    private bool TrySetFearLevel(Entity<FearComponent?> ent, FearState state)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.State == state)
            return true;

        ent.Comp.State = state;
        Dirty(ent);

        return true;
    }

    private void SetShaderStrength<T>(Entity<T?> ent, float currentRange, float maxRange, MinMaxExtended parameters)
        where T : IShaderStrength, IComponent
    {
        var strength = CalculateShaderStrength(currentRange, maxRange, parameters);
        _shaderStrength.TrySetAdditionalStrength(ent, strength);
    }

    private static float CalculateShaderStrength(float currentRange, float maxRange, MinMaxExtended parameters)
    {
        if (currentRange <= 0f)
            return parameters.Max;

        var proximityFactor = 1f - Math.Clamp(currentRange / maxRange, 0f, 1f);

        return MathHelper.Lerp(parameters.Min, parameters.Max, proximityFactor);
    }
}
