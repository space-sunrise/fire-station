using Content.Server.GameTicking.Rules.VariationPass;
using Content.Server._Scp.GameTicking.Rules.VariationPass.Components;
using Content.Server._Scp.GameTicking.Rules.VariationPass.Components.ReplacementMarkers;

namespace Content.Server._Scp.GameTicking.Rules.VariationPass;

/// <summary>
/// This handles the ability to replace entities marked with <see cref="WallReplacementMarkerComponent"/> in a variation pass
/// </summary>
public sealed class WallConcreteScienceReplaceVariationPassSystem : BaseEntityReplaceVariationPassSystem<WallConcreteScienceReplacementMarkerComponent, WallConcreteScienceReplaceVariationPassComponent>
{
}
