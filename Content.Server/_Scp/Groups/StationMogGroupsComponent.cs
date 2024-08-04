using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Scp.Groups;

[RegisterComponent]
public sealed partial class StationMogGroupsComponent : Component
{
    [DataField("allowedGroups")]
    public List<string> AllowedGroups;
}
