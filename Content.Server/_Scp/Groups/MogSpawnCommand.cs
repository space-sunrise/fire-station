using System.Linq;
using Content.Server.Administration;
using Content.Server.Commands;
using Content.Shared._Scp.Groups;
using Content.Shared.Administration;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Scp.Groups;

[AdminCommand(AdminFlags.Round)]
public sealed class MogSpawnCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entMan = default!;

    public string Command => "sendmog";

    public string Description => Loc.GetString("send-mog-command-description");

    public string Help => Loc.GetString("send-mog-command-help-text", ("command", Command));

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var euidNet) || !_entMan.TryGetEntity(euidNet, out var euid))
        {
            shell.WriteError($"Failed to parse euid '{args[0]}'.");
            return;
        }

        var protoId = args[1];
        var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
        if (!prototypeManager.TryIndex<MogGroupPrototype>(protoId, out var proto))
        {
            shell.WriteError($"No Mobile Operative Group found with ID {protoId}");
            return;
        }

        var mogSpawnSystem = IoCManager.Resolve<IEntityManager>().System<MogSpawnSystem>();
        if (!mogSpawnSystem.TrySpawnGroup(euid.Value, protoId))
        {
            shell.WriteError("Mobile Operative Group was not sent");
        }
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        switch (args.Length)
        {
            case 1:
                var stations = ContentCompletionHelper.StationIds(_entMan);
                return CompletionResult.FromHintOptions(stations, "[StationId]");
            case 2:
                var options = IoCManager.Resolve<IPrototypeManager>()
                    .EnumeratePrototypes<MogGroupPrototype>()
                    .Select(p => new CompletionOption(p.ID));
                return CompletionResult.FromHintOptions(options, Loc.GetString("send-mog-group-command-arg-id"));
        }
        return CompletionResult.Empty;
    }
}
