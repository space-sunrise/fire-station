using Content.Shared.Parallax.Biomes;
using Content.Shared.Weather;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server._Scp.Other.RandomPlanet;

/// <summary>
/// Компонент, создающий на станции случайный биом из списка.
/// </summary>
[RegisterComponent]
public sealed partial class RandomPlanetComponent : Component
{
    [DataField(required: true)]
    public List<StationPlanetData> Data = [];
}

[DataDefinition, Serializable]
public sealed partial class StationPlanetData
{
    [DataField] public ProtoId<BiomeTemplatePrototype> Biome;
    [DataField] public List<ProtoId<WeatherPrototype>>? Weather;
    [DataField] public List<int>? Seed;
}
