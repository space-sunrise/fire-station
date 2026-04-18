using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Light.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.MetaGarbage;

/// <summary>
/// JSON-serializable DTOs for DB persistence of MetaGarbage data.
/// Avoids engine type serialization issues with Vector2, Angle, ReagentId etc.
/// </summary>
public static class MetaGarbageSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(List<StationMetaGarbageData> data)
    {
        var dtos = data.Select(ToDto).ToList();
        return JsonSerializer.Serialize(dtos, Options);
    }

    public static List<StationMetaGarbageData> Deserialize(string json)
    {
        var dtos = JsonSerializer.Deserialize<List<MetaGarbageDataDto>>(json, Options);
        if (dtos == null)
            return [];

        return dtos.Select(FromDto).ToList();
    }

    private static MetaGarbageDataDto ToDto(StationMetaGarbageData data)
    {
        return new MetaGarbageDataDto
        {
            Prototype = data.Prototype.Id,
            X = data.Position.X,
            Y = data.Position.Y,
            Rotation = data.Rotation.Theta,
            Replace = data.Replace,
            ContainerName = data.ContainerName,
            BulbState = data.BulbState.HasValue ? (int)data.BulbState.Value : null,
            LiquidData = data.LiquidData?.ToDictionary(
                kvp => kvp.Key,
                kvp => new MetaGarbageSolutionDto
                {
                    Contents = kvp.Value.Contents.Select(r => new MetaGarbageReagentDto
                    {
                        Prototype = r.Reagent.Prototype,
                        Quantity = r.Quantity.Float()
                    }).ToList()
                }),
            ExtraData = data.ExtraData
        };
    }

    private static StationMetaGarbageData FromDto(MetaGarbageDataDto dto)
    {
        Dictionary<string, MetaGarbageSolutionProxy>? liquid = null;
        if (dto.LiquidData != null)
        {
            liquid = dto.LiquidData.ToDictionary(
                kvp => kvp.Key,
                kvp => new MetaGarbageSolutionProxy(
                    kvp.Value.Contents.Select(r => new MetaGarbageReagentQuantityProxy(
                        new ReagentId(new ProtoId<ReagentPrototype>(r.Prototype), null),
                        FixedPoint2.New(r.Quantity)
                    )).ToList()
                ));
        }

        LightBulbState? bulbState = dto.BulbState.HasValue
            ? (LightBulbState)dto.BulbState.Value
            : null;

        return new StationMetaGarbageData(
            new EntProtoId(dto.Prototype),
            new Vector2(dto.X, dto.Y),
            new Angle(dto.Rotation),
            liquid,
            dto.Replace,
            dto.ContainerName,
            bulbState,
            dto.ExtraData);
    }
}

public sealed class MetaGarbageDataDto
{
    public string Prototype { get; set; } = string.Empty;
    public float X { get; set; }
    public float Y { get; set; }
    public double Rotation { get; set; }
    public Dictionary<string, MetaGarbageSolutionDto>? LiquidData { get; set; }
    public bool Replace { get; set; }
    public string? ContainerName { get; set; }
    public int? BulbState { get; set; }
    public Dictionary<string, JsonElement>? ExtraData { get; set; }
}

public sealed class MetaGarbageSolutionDto
{
    public List<MetaGarbageReagentDto> Contents { get; set; } = [];
}

public sealed class MetaGarbageReagentDto
{
    public string Prototype { get; set; } = string.Empty;
    public float Quantity { get; set; }
}

public sealed class MetaGarbageFileSave
{
    public int MapVersion { get; set; }
    public DateTime SavedAt { get; set; }
    public string Data { get; set; } = string.Empty;
}
