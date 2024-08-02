using Content.Server.Maps.NameGenerators;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server._Scp.NameGenerators;

[UsedImplicitly]
public sealed partial class ScpSiteNameGenerator : StationNameGenerator
{
    [DataField("prefixCreator")] public string PrefixCreator = default!;

    public override string FormatName(string input)
    {
        var random = IoCManager.Resolve<IRobustRandom>();

        return string.Format(input, $"{random.Next(1, 83):D2}");
    }
}
