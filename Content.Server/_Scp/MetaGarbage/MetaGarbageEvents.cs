using System.Text.Json;

namespace Content.Server._Scp.MetaGarbage;

/// <summary>
/// Raised on an entity being saved
/// </summary>
[ByRefEvent]
public record struct MetaGarbageSaveEvent(Dictionary<string, JsonElement> ExtraData);

/// <summary>
/// Raised on a freshly spawned entity during restore. Read ExtraData to restore state
/// </summary>
[ByRefEvent]
public record struct MetaGarbageRestoreEvent(Dictionary<string, JsonElement> ExtraData);
