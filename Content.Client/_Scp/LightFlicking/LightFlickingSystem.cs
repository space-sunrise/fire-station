using Content.Client.Light.Visualizers;
using Content.Shared._Scp.LightFlicking;
using Content.Shared.Light;
using Robust.Client.GameObjects;

namespace Content.Client._Scp.LightFlicking;

public sealed class LightFlickingSystem : SharedLightFlickingSystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    private const float RadiusVariationPercentage = 0.2f;
    private const float EnergyVariationPercentage = 0.2f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = AllEntityQuery<LightFlickingComponent, PoweredLightVisualsComponent>();

        while (query.MoveNext(out var uid, out var flicking, out _))
        {
            if (!flicking.Enabled)
                continue;

            if (Timing.CurTime <= flicking.NextFlickTime)
                continue;

            var newEnergy = Variantize(flicking.DumpedEnergy, EnergyVariationPercentage);
            var newRadius = Variantize(flicking.DumpedRadius, RadiusVariationPercentage);
            _pointLight.SetEnergy(uid, newEnergy);
            _pointLight.SetRadius(uid, newRadius);

            // Изменяем цвет лампочки в зависимости от того, насколько сильно изменилось свечение
            var newColor = DimColorBasedOnChange(flicking.DumpedColor, flicking.DumpedEnergy, newEnergy);
            // _pointLight.SetColor(uid, newColor); слишком вырвиглазно, но работает

            if (TryComp<SpriteComponent>(uid, out var spriteComponent))
                spriteComponent.LayerSetColor(PoweredLightLayers.Glow, newColor);

            SetNextFlickingTime((uid, flicking));
        }
    }

    private float Variantize(float origin, float baseVariation)
    {
        var variation = (float)(Random.NextDouble() * (2 * baseVariation) - baseVariation);
        return origin * (1 + variation);
    }

    // Мне это написал чатгпт
    private Color DimColorBasedOnChange(Color color, float firstNumber, float secondNumber)
    {
        // Вычисляем разницу между вторым и первым числом
        var difference = Math.Abs(secondNumber - firstNumber);

        // Нормализуем разницу в диапазоне от 0 до 1
        var normalizedDifference = Math.Clamp(difference / firstNumber, 0, 1);

        // Уменьшаем яркость цвета на основе нормализованной разницы
        var dimFactor = 1 - normalizedDifference;

        // Применяем уменьшение яркости к каждому компоненту цвета
        var r = color.R * dimFactor;
        var g = color.G * dimFactor;
        var b = color.B * dimFactor;

        // Возвращаем новый цвет с уменьшенной яркостью
        return new Color(r, g, b, color.A);
    }
}
