using System.Linq;
using System.Text;
using Content.Server.Administration;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Shared.Administration;
using Content.Shared.Item;
using Robust.Shared.Console;

namespace Content.Server._Scp.Research.Artifacts.Effects.CreateRandomArtifacts;

public sealed partial class ArtifactCreateRandomArtifactsSystem
{
    [AdminCommand(AdminFlags.Admin)]
    private void ListArtifacts(IConsoleShell shell, string argstr, string[] args)
    {
        var items = EntityQuery<ArtifactComponent, ItemComponent>();
        var msg = new StringBuilder();

        foreach (var (artifact, _) in items)
        {
            var entity = artifact.Owner;
            var effects = string.Join(", ", artifact.NodeTree.Select(x => x.Effect));

            msg.AppendLine($"{Name(entity)}: {effects}, {entity}\n");
        }

        shell.WriteLine(msg.ToString());
    }

}
