using System.Numerics;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Light.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.MetaGarbage;

/// <summary>
/// Компонент вешающийся на станцию, позволяющей ей сохранять и переносить между раундами мусор и жидкости.
/// </summary>
[RegisterComponent]
public sealed partial class MetaGarbageTargetComponent : Component
{
    /// <summary>
    /// Какое процентное соотношение от общего числа собранного в прошлом раунде мусора вернется в новом раунде?
    /// </summary>
    [DataField]
    public float SpawnPercent = 100f;

    /// <summary>
    /// Модификаторы количества луж/следов с заданными реагентами.
    /// Ключ - реагент, наличие которого будет требовать броска кубика.
    /// Значение - шанс, что сущность с этим реагентом будет заспавнена.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<ReagentPrototype>, float> ReagentSaveModifiers;
}

/// <summary>
/// Набор параметров, которые хранят данные о сохраненном предмете-мусоре для переспавна в следующем раунде.
/// </summary>
/// <param name="Prototype">Айди прототипа</param>
/// <param name="Position">Позиция в мире</param>
/// <param name="Rotation">Угол поворота</param>
/// <param name="LiquidData">Реагенты, хранящиеся в предметы(для луж)</param>
public record struct StationMetaGarbageData(
    EntProtoId Prototype,
    Vector2 Position,
    Angle Rotation,
    Dictionary<string, MetaGarbageSolutionProxy>? LiquidData,
    bool Replace = false,
    string? ContainerName = null,
    LightBulbState? BulbState = null
    )
{
    public readonly EntProtoId Prototype = Prototype;
    public readonly Vector2 Position = Position;
    public readonly Angle Rotation = Rotation;
    public readonly Dictionary<string, MetaGarbageSolutionProxy>? LiquidData = LiquidData;
    public readonly bool Replace = Replace;
    public readonly string? ContainerName = ContainerName;
    public readonly LightBulbState? BulbState = BulbState;
}

// Все что находится ниже существует потому, что блядская система реагентов такая блядская
// Неким образом после рестарта раунда стандартные структуры содержащие информацию о реагентах теряют все знания об объеме жидкостей
// Она просто блять исчезает в моменте. При сохранении в словарь она есть, в следующем раунде ее уже нет.
// Поэтому я написал прокси методы, хранящие базовую информацию для воссоздания этих структур из раунда в раунд

/// <summary>
/// Структура воссоздающая данные для создания <seealso cref="Solution"/>
/// </summary>
public record struct MetaGarbageSolutionProxy(List<MetaGarbageReagentQuantityProxy> Contents)
{
    public readonly List<MetaGarbageReagentQuantityProxy> Contents = Contents;
}

/// <summary>
/// Структура воссоздающая данные для создания <seealso cref="ReagentQuantity"/>
/// </summary>
public record struct MetaGarbageReagentQuantityProxy(ReagentId Reagent, FixedPoint2 Quantity)
{
    public readonly ReagentId Reagent = Reagent;
    public readonly FixedPoint2 Quantity = Quantity;
}
