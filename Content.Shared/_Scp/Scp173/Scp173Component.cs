using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp173;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class Scp173Component : Component
{
    [DataField]
    public float WatchRange = 12f;

    #region Fast movement action

    [DataField]
    public float MaxJumpRange = 4f;

    [DataField]
    public int MaxWatchers = 1;

    #endregion

    #region Blind action

    [DataField]
    public TimeSpan StartBlindTime = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan BlindnessTime = TimeSpan.FromSeconds(7);

    #endregion

    [DataField]
    public SoundSpecifier NeckSnapSound = new SoundCollectionSpecifier("Scp173NeckSnap");

    [DataField]
    public SoundSpecifier TeleportationSound = new SoundCollectionSpecifier("FootstepScp173Classic");

    [AutoNetworkedField]
    public FixedPoint2 ReagentVolumeAround;

    [ViewVariables]
    public static readonly ProtoId<ReagentPrototype> Reagent = "Scp173Reagent";

    [ViewVariables]
    public const int MinTotalSolutionVolume = 500;

    [ViewVariables]
    public const int ExtraMinTotalSolutionVolume = 800;
}
