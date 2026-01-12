using Content.Shared.FixedPoint;
using Content.Shared.StatusIcon;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp096.Main.Components;

/// <summary>
/// Компонент, отмечающий сущность как цель скромника.
/// <seealso cref="Scp096Component"/>
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp096TargetComponent : Component
{
    /// <summary>
    /// Иконка, которую будет видеть скромник над владельцем компонента.
    /// </summary>
    [DataField]
    public ProtoId<FactionIconPrototype> KillIconPrototype = "Scp096TargetIcon";

    /// <summary>
    /// Количество урона, которое нужно нанести скромнику, чтобы перестать считать объект как цель.
    /// После этого компонент удаляется с сущности.
    /// </summary>
    [DataField]
    public FixedPoint2 TotalDamageToStop = FixedPoint2.New(500);

    /// <summary>
    /// Количество урона, которое нанес конкретно скромник своей цели.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 AlreadyAppliedDamage = FixedPoint2.Zero;

    /// <summary>
    /// Звук, проигрывающийся жертве, когда она видит скромника.
    /// </summary>
    [DataField]
    public SoundSpecifier SeenSound = new SoundPathSpecifier("/Audio/_Scp/Scp096/seen.ogg", AudioParams.Default.WithVolume(3f));
}
