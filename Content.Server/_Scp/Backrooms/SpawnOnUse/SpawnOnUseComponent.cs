using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Backrooms.SpawnOnUse;

[RegisterComponent]
public sealed partial class SpawnOnUseComponent : Component
{
    [DataField(required: true), ViewVariables]
    public HashSet<EntProtoId> Entities = new();

    [DataField]
    public int Charges = 1;

    #region Effects, Sounds and Strings

    [DataField]
    public SoundSpecifier? SoundSuccessFul;

    [DataField]
    public string? PopupNoCharges;

    #endregion


}
