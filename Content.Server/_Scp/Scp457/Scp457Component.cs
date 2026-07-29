using Content.Shared.Damage;

namespace Content.Server._Scp.Scp457;

[RegisterComponent]
public sealed partial class Scp457Component : Component
{
    [DataField]
    public float ObjectSize = 1f;

    [DataField]
    public float MinimumObjectSize = 0.4f;

    [DataField]
    public float ObjectSizeLimit = 2f;

    [DataField]
    public float ObjectSizeFlammableAdd = 0.1f;

    [DataField]
    public float ObjectSizeDecay = 0.01f;

    [DataField]
    public float SmallFormSize = 0.65f;

    [DataField]
    public float StructuralBreakSize = 1.5f;

    [DataField]
    public float StructuralDamage = 100f;

    [DataField]
    public float DamageModifier = 1f;

    [DataField]
    public float RegenerationModifier = 1f;

    [DataField]
    public float DamageModifierFlammableAdd = 0.1f;

	[DataField]
    public float ObjectWaterSizeDecrease = 0.01f;

    [DataField]
    public float RegenerationModifierFlammableAdd = 0.1f;

    [DataField]
    public float DamageModifierLimit = 3f;

    [DataField]
    public float RegenerationModifierLimit = 2f;

    [DataField]
    public string BodyFixtureId = "fix1";

    [DataField]
    public HashSet<string> ReactiveGroupsWhitelist = ["Flammable"];

    [DataField]
    public HashSet<string> FlammableMaterialsWhitelist = ["Wood", "Paper", "Cardboard", "Cloth", "Carpet"];

    [ViewVariables]
    public float AppliedObjectSize = 1f;

    [ViewVariables]
    public DamageSpecifier? BasePassiveDamage;

    [ViewVariables]
    public TimeSpan? NextChangeObjectSize;
}
