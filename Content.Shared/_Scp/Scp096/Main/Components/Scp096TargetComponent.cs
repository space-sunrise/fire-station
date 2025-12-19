using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096TargetComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype> KillIconPrototype = "Scp096TargetIcon";

    [DataField]
    public FixedPoint2 TotalDamageToStop = FixedPoint2.New(500);

    [DataField, AutoNetworkedField]
    public FixedPoint2 AlreadyAppliedDamage = FixedPoint2.Zero;

    /// <summary>
    /// Звук, проигрывающийся жертве, когда она видит скромника.
    /// </summary>
    [DataField]
    public SoundSpecifier SeenSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/seen.ogg", AudioParams.Default.WithVolume(3f));
}
