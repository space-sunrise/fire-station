using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp999;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp999Component : Component
{
    #region Abilities

    [DataField, AutoNetworkedField]
    public Scp999States CurrentState = Scp999States.Default;

    [DataField]
    public Dictionary<Scp999States, string> States = new();

    #endregion

    #region Feeding

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float CreateJellyChance = 0.2f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EntProtoId Scp999Jelly = "FoodJellyScp999";

    [DataField]
    public SoundSpecifier? CreateJellySound;

    [DataField]
    public ProtoId<TagPrototype> CandyTag = "Sweetness";

    #endregion
}
