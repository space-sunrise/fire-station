using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp999;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp999Component : Component
{
    #region Abilities

    [ViewVariables, AutoNetworkedField]
    public Scp999States CurrentState = Scp999States.Default;

    [DataField]
    public Dictionary<Scp999States, string> States = new();

    [DataField]
    public SoundSpecifier? WallSound = new SoundCollectionSpecifier("WallTransformScp999");

    [DataField]
    public SoundSpecifier? SleepSound = new SoundPathSpecifier("/Audio/_Scp/Scp999/sleep.ogg");

    [DataField]
    public FixedPoint2 TotalDamageToChangeState = 30f;

    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CurrentTotalDamage = FixedPoint2.Zero;

    #endregion

    #region Feeding

    [DataField]
    public float CreateJellyChance = 0.2f;

    [DataField]
    public EntProtoId Scp999Jelly = "FoodJellyScp999";

    [DataField]
    public SoundSpecifier? CreateJellySound;

    [DataField]
    public ProtoId<TagPrototype> CandyTag = "Sweetness";

    #endregion
}
