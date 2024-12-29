using Content.Shared.Roles;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server._Sunrise.Roles;


[RegisterComponent]
public sealed partial class RelativeJobsCountComponent : Component
{
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, Dictionary<ProtoId<JobPrototype>, int>> Jobs = new ();
}
