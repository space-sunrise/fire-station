using Content.Shared.Parallax.Biomes;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Other.RandomPlanet;

/// <summary>
/// Компонент, создающий на станции случайный биом из списка.
/// </summary>
[RegisterComponent]
public sealed partial class RandomPlanetComponent : Component
{
    /// <summary>
    /// <see cref="StationPlanetData"/>
    /// </summary>
    [DataField(required: true)]
    public List<StationPlanetData> Data = [];
}

/// <summary>
/// Данные, нужные для генерации биома и погоды у комплекса.
/// Хранятся в качестве отдельной структуры для удобства работы и сериализации.
/// </summary>
[DataDefinition, Serializable]
public sealed partial class StationPlanetData
{
    /// <summary>
    /// Прототип биома, который будет использован.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<BiomeTemplatePrototype> Biome;

    /// <summary>
    /// Прототипы погоды, которые могут быть использованы.
    /// Если ничего не указано - погоды не будет.
    /// Если указано будет выбрана случайная погода
    /// </summary>
    [DataField]
    public List<ProtoId<WeatherPrototype>>? Weather;

    /// <summary>
    /// Зерна генерации биомов.
    /// Если не задано будет выбрано случайное.
    /// Если задано - случайное из списка.
    /// </summary>
    [DataField]
    public List<int>? Seed;
}
