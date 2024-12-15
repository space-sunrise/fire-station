using Robust.Shared.Prototypes;

namespace Content.Shared.Research;

[Prototype("researchPoint")]
public sealed class ResearchPointPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [DataField(required: true)]
    public LocId Name { get; set; }
}
