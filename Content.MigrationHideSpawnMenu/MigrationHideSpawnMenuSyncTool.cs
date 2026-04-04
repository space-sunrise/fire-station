using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using YamlDotNet.RepresentationModel;

namespace Content.MigrationHideSpawnMenu;

/// <summary>
/// Synchronizes <c>HideSpawnMenu</c> category for migrated entity prototypes.
/// </summary>
public static class MigrationHideSpawnMenuSyncTool
{
    /// <summary>
    /// Environment variable used for edit comment text appended to updated lines.
    /// </summary>
    public const string EditCommentEnvironmentVariable = "MIGRATION_HIDE_SPAWN_EDIT_COMMENT";

    /// <summary>
    /// Fallback edit comment used when the environment variable is empty.
    /// </summary>
    public const string FallbackEditComment = "Fire edit";

    private const string HideSpawnMenuCategory = "HideSpawnMenu";
    private const string MigrationRelativePath = "Resources/migration.yml";
    private const string PrototypesRelativePath = "Resources/Prototypes";
    private const string CommandPrefix = "hide-spawn-menu";

    public const int SuccessExitCode = 0;
    public const int CheckOutOfSyncExitCode = 1;
    public const int TechnicalFailureExitCode = 2;

    /// <summary>
    /// Runs the tool from the current working directory.
    /// </summary>
    public static int Run(string[] args)
    {
        return Run(args, Directory.GetCurrentDirectory(), null);
    }

    /// <summary>
    /// Runs the tool with explicit repository root and optional edit comment override.
    /// </summary>
    public static int Run(string[] args, string repositoryRoot, string editCommentOverride)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (!TryParseMode(args, out var mode))
            {
                PrintUsage();
                return TechnicalFailureExitCode;
            }

            var editComment = ResolveEditComment(editCommentOverride);
            var summary = Execute(mode, repositoryRoot, editComment);
            stopwatch.Stop();
            PrintSummary(mode, summary, stopwatch.Elapsed);

            switch (mode)
            {
                case MigrationHideSpawnMenuMode.Check when summary.Violations.Count > 0:
                    foreach (var violation in summary.Violations)
                    {
                        Console.WriteLine(violation);
                    }

                    return CheckOutOfSyncExitCode;

                case MigrationHideSpawnMenuMode.Sync:
                    return SuccessExitCode;

                default:
                    return SuccessExitCode;
            }
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            Console.Error.WriteLine(e);
            Console.Error.WriteLine($"❌ Failed in {stopwatch.Elapsed.TotalMilliseconds:N0} ms.");
            return TechnicalFailureExitCode;
        }
    }

    private static MigrationHideSpawnMenuSummary Execute(
        MigrationHideSpawnMenuMode mode,
        string repositoryRoot,
        string editComment)
    {
        repositoryRoot = ResolveRepositoryRoot(repositoryRoot);
        var migrationPath = Path.Combine(repositoryRoot, MigrationRelativePath);
        var prototypesRoot = Path.Combine(repositoryRoot, PrototypesRelativePath);

        if (!Directory.Exists(prototypesRoot))
            throw new DirectoryNotFoundException($"Prototype directory was not found: {prototypesRoot}");

        var migrationMappings = ReadMigrationMappings(migrationPath);
        var sourceIds = new HashSet<string>(migrationMappings.Keys, StringComparer.Ordinal);
        var summary = new MigrationHideSpawnMenuSummary();
        var parsedFiles = LoadPrototypeFiles(prototypesRoot);
        summary.FilesScanned = parsedFiles.Count;

        var graphEntityMap = BuildConstructionGraphEntityMap(parsedFiles);
        var resolvedConstructionBlocks = ResolveConstructionBlocks(parsedFiles, graphEntityMap);
        var visibleConstructionResults = resolvedConstructionBlocks
            .Where(static resolved => !IsConstructionHidden(resolved.Block))
            .Select(static resolved => resolved.ResultEntityId)
            .ToHashSet(StringComparer.Ordinal);
        var constructionBlocksByFile = resolvedConstructionBlocks
            .GroupBy(static resolved => resolved.File)
            .ToDictionary(static group => group.Key, static group => group.ToList());

        foreach (var file in parsedFiles)
        {
            var pendingCandidates = new List<PendingCandidate>();

            foreach (var block in file.Parsed.EntityBlocks)
            {
                if (!IsEntityCandidate(sourceIds, block))
                    continue;

                summary.CandidatesFound++;
                pendingCandidates.Add(new PendingCandidate(
                    block.StartLineIndex,
                    block.Id,
                    () => AddHideSpawnMenuCategory(file.Parsed.Lines, block, editComment)));
            }

            if (constructionBlocksByFile.TryGetValue(file, out var resolvedBlocks))
            {
                foreach (var resolvedBlock in resolvedBlocks)
                {
                    if (!IsConstructionCandidate(migrationMappings, visibleConstructionResults, resolvedBlock))
                        continue;

                    summary.CandidatesFound++;
                    pendingCandidates.Add(new PendingCandidate(
                        resolvedBlock.Block.StartLineIndex,
                        resolvedBlock.Block.Id,
                        () => EnsureConstructionHidden(file.Parsed.Lines, resolvedBlock.Block, editComment)));
                }
            }

            if (pendingCandidates.Count == 0)
                continue;

            if (mode == MigrationHideSpawnMenuMode.Check)
            {
                foreach (var candidate in pendingCandidates)
                {
                    summary.Violations.Add($"{Path.GetRelativePath(repositoryRoot, file.Path)}: {candidate.ViolationId}");
                }

                continue;
            }

            foreach (var candidate in pendingCandidates.OrderByDescending(static candidate => candidate.StartLineIndex))
            {
                candidate.ApplyChange();
                summary.CandidatesUpdated++;
            }

            var updatedContent = MigrationHideSpawnMenuPrototypeParser.ComposeContent(file.Parsed);
            if (string.Equals(updatedContent, file.OriginalContent, StringComparison.Ordinal))
                continue;

            File.WriteAllText(file.Path, updatedContent);
            summary.FilesChanged++;
        }

        return summary;
    }

    private static List<MigrationHideSpawnMenuRepositoryFile> LoadPrototypeFiles(string prototypesRoot)
    {
        var files = new List<MigrationHideSpawnMenuRepositoryFile>();

        foreach (var prototypePath in Directory.EnumerateFiles(prototypesRoot, "*.yml", SearchOption.AllDirectories)
                     .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase))
        {
            var originalContent = File.ReadAllText(prototypePath);
            var parsed = MigrationHideSpawnMenuPrototypeParser.Parse(originalContent);
            files.Add(new MigrationHideSpawnMenuRepositoryFile(prototypePath, originalContent, parsed));
        }

        return files;
    }

    private static Dictionary<ConstructionGraphNodeKey, string> BuildConstructionGraphEntityMap(
        List<MigrationHideSpawnMenuRepositoryFile> parsedFiles)
    {
        var result = new Dictionary<ConstructionGraphNodeKey, string>();

        foreach (var file in parsedFiles)
        {
            foreach (var graphBlock in file.Parsed.ConstructionGraphBlocks)
            {
                if (!graphBlock.HasId)
                    continue;

                foreach (var (nodeId, entityId) in graphBlock.NodeEntities)
                {
                    result[new ConstructionGraphNodeKey(graphBlock.Id, nodeId)] = entityId;
                }
            }
        }

        return result;
    }

    private static List<MigrationHideSpawnMenuResolvedConstructionBlock> ResolveConstructionBlocks(
        List<MigrationHideSpawnMenuRepositoryFile> parsedFiles,
        Dictionary<ConstructionGraphNodeKey, string> graphEntityMap)
    {
        var result = new List<MigrationHideSpawnMenuResolvedConstructionBlock>();

        foreach (var file in parsedFiles)
        {
            foreach (var block in file.Parsed.ConstructionBlocks)
            {
                if (!block.HasId || !block.HasGraph || !block.HasTargetNode)
                    continue;

                if (!graphEntityMap.TryGetValue(new ConstructionGraphNodeKey(block.GraphId, block.TargetNode), out var entityId))
                    continue;

                result.Add(new MigrationHideSpawnMenuResolvedConstructionBlock(file, block, entityId));
            }
        }

        return result;
    }

    private static string ResolveRepositoryRoot(string repositoryRoot)
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRoot(repositoryRoot);
        AddRoot(Directory.GetCurrentDirectory());
        AddRoot(AppContext.BaseDirectory);

        foreach (var root in roots)
        {
            var resolved = TryResolveRepositoryRoot(root);
            if (resolved != null)
                return resolved;
        }

        throw new FileNotFoundException(
            $"Migration file was not found. Searched upward from: {string.Join(", ", roots)}");

        void AddRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
                return;

            roots.Add(Path.GetFullPath(root));
        }
    }

    private static string TryResolveRepositoryRoot(string startDirectory)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current != null)
        {
            var migrationPath = Path.Combine(current.FullName, MigrationRelativePath);
            if (File.Exists(migrationPath))
                return current.FullName;

            current = current.Parent;
        }

        return null;
    }

    private static bool IsEntityCandidate(HashSet<string> sourceIds, MigrationHideSpawnMenuEntityBlock block)
    {
        if (!block.HasId || block.IsAbstract)
            return false;

        if (!sourceIds.Contains(block.Id))
            return false;

        foreach (var category in block.Categories)
        {
            if (category.Equals(HideSpawnMenuCategory, StringComparison.Ordinal))
                return false;
        }

        return true;
    }

    private static bool IsConstructionCandidate(
        Dictionary<string, string> migrationMappings,
        HashSet<string> visibleConstructionResults,
        MigrationHideSpawnMenuResolvedConstructionBlock resolvedBlock)
    {
        if (!resolvedBlock.Block.HasId || IsConstructionHidden(resolvedBlock.Block))
            return false;

        if (!migrationMappings.TryGetValue(resolvedBlock.ResultEntityId, out var replacementEntityId))
            return false;

        if (string.IsNullOrWhiteSpace(replacementEntityId))
            return false;

        return visibleConstructionResults.Contains(replacementEntityId);
    }

    private static bool IsConstructionHidden(MigrationHideSpawnMenuConstructionBlock block)
    {
        return block.HasHide && block.Hide;
    }

    private static void AddHideSpawnMenuCategory(
        List<string> lines,
        MigrationHideSpawnMenuEntityBlock block,
        string editComment)
    {
        switch (block.CategoryKind)
        {
            case MigrationHideSpawnMenuCategoryKind.Inline:
                UpdateInlineCategories(lines, block, editComment);
                break;
            case MigrationHideSpawnMenuCategoryKind.Block:
                InsertBlockCategory(lines, block, editComment);
                break;
            default:
                InsertMissingCategories(lines, block, editComment);
                break;
        }
    }

    private static void EnsureConstructionHidden(
        List<string> lines,
        MigrationHideSpawnMenuConstructionBlock block,
        string editComment)
    {
        if (block.HasHide && block.Hide)
            return;

        if (block.HasHide && block.HideLineIndex >= 0 && block.HideLineIndex < lines.Count)
        {
            UpdateConstructionHide(lines, block, editComment);
            return;
        }

        InsertMissingConstructionHide(lines, block, editComment);
    }

    private static void UpdateInlineCategories(
        List<string> lines,
        MigrationHideSpawnMenuEntityBlock block,
        string editComment)
    {
        if (block.CategoriesLineIndex < 0 || block.CategoriesLineIndex >= lines.Count)
        {
            InsertMissingCategories(lines, block, editComment);
            return;
        }

        var categories = new List<string>(block.Categories.Count + 1);
        categories.AddRange(block.Categories);
        categories.Add(HideSpawnMenuCategory);

        var indent = block.FieldIndent > -1 ? block.FieldIndent : block.PrototypeIndent + 2;
        var indentValue = new string(' ', indent);
        lines[block.CategoriesLineIndex] = $"{indentValue}categories: [ {string.Join(", ", categories)} ] # {editComment}";
    }

    private static void InsertBlockCategory(
        List<string> lines,
        MigrationHideSpawnMenuEntityBlock block,
        string editComment)
    {
        if (block.CategoriesLineIndex < 0 || block.CategoriesLineIndex >= lines.Count)
        {
            InsertMissingCategories(lines, block, editComment);
            return;
        }

        var itemIndent = block.CategoryItemIndent > -1
            ? block.CategoryItemIndent
            : (block.FieldIndent > -1 ? block.FieldIndent + 2 : block.PrototypeIndent + 2);
        var insertAt = block.CategoryItemLineIndices.Count > 0
            ? block.CategoryItemLineIndices[^1] + 1
            : block.CategoriesLineIndex + 1;

        var indentValue = new string(' ', itemIndent);
        lines.Insert(insertAt, $"{indentValue}- {HideSpawnMenuCategory} # {editComment}");
    }

    private static void InsertMissingCategories(
        List<string> lines,
        MigrationHideSpawnMenuEntityBlock block,
        string editComment)
    {
        var indent = block.FieldIndent > -1 ? block.FieldIndent : block.PrototypeIndent + 2;
        var insertAt = block.IdLineIndex > -1 ? block.IdLineIndex + 1 : block.StartLineIndex + 1;
        var indentValue = new string(' ', indent);

        lines.Insert(insertAt, $"{indentValue}categories: [ {HideSpawnMenuCategory} ] # {editComment}");
    }

    private static void UpdateConstructionHide(
        List<string> lines,
        MigrationHideSpawnMenuConstructionBlock block,
        string editComment)
    {
        var indent = block.FieldIndent > -1 ? block.FieldIndent : block.PrototypeIndent + 2;
        var indentValue = new string(' ', indent);
        lines[block.HideLineIndex] = $"{indentValue}hide: true # {editComment}";
    }

    private static void InsertMissingConstructionHide(
        List<string> lines,
        MigrationHideSpawnMenuConstructionBlock block,
        string editComment)
    {
        var indent = block.FieldIndent > -1 ? block.FieldIndent : block.PrototypeIndent + 2;
        var indentValue = new string(' ', indent);
        var insertAt = block.TargetNodeLineIndex > -1
            ? block.TargetNodeLineIndex + 1
            : block.GraphLineIndex > -1
                ? block.GraphLineIndex + 1
                : block.IdLineIndex > -1
                    ? block.IdLineIndex + 1
                    : block.StartLineIndex + 1;

        lines.Insert(insertAt, $"{indentValue}hide: true # {editComment}");
    }

    private static Dictionary<string, string> ReadMigrationMappings(string migrationPath)
    {
        using var reader = new StreamReader(migrationPath);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var document in yaml.Documents)
        {
            if (document.RootNode is not YamlMappingNode map)
                continue;

            foreach (var pair in map.Children)
            {
                if (pair.Key is not YamlScalarNode oldIdNode)
                    continue;

                if (pair.Value is not YamlScalarNode newIdNode)
                    continue;

                var oldId = oldIdNode.Value?.Trim();
                var newId = newIdNode.Value?.Trim();

                if (string.IsNullOrWhiteSpace(oldId))
                    continue;

                if (string.IsNullOrWhiteSpace(newId) || newId.Equals("null", StringComparison.OrdinalIgnoreCase))
                {
                    result[oldId] = null;
                    continue;
                }

                result[oldId] = newId;
            }
        }

        return result;
    }

    private static string ResolveEditComment(string editCommentOverride)
    {
        if (!string.IsNullOrWhiteSpace(editCommentOverride))
            return editCommentOverride.Trim();

        var fromEnvironment = Environment.GetEnvironmentVariable(EditCommentEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
            return fromEnvironment.Trim();

        return FallbackEditComment;
    }

    private static bool TryParseMode(string[] args, out MigrationHideSpawnMenuMode mode)
    {
        mode = default;

        if (args.Length == 0)
        {
            mode = MigrationHideSpawnMenuMode.Sync;
            return true;
        }

        if (args.Length == 1)
            return TryParseMode(args[0], out mode);

        if (args.Length == 2 && args[0].Equals(CommandPrefix, StringComparison.OrdinalIgnoreCase))
            return TryParseMode(args[1], out mode);

        return false;
    }

    private static bool TryParseMode(string value, out MigrationHideSpawnMenuMode mode)
    {
        mode = default;

        if (value.Equals("sync", StringComparison.OrdinalIgnoreCase))
        {
            mode = MigrationHideSpawnMenuMode.Sync;
            return true;
        }

        if (value.Equals("check", StringComparison.OrdinalIgnoreCase))
        {
            mode = MigrationHideSpawnMenuMode.Check;
            return true;
        }

        return false;
    }

    private static void PrintSummary(MigrationHideSpawnMenuMode mode, MigrationHideSpawnMenuSummary summary, TimeSpan elapsed)
    {
        Console.WriteLine("🧭 HideSpawnMenu migration sync report");
        Console.WriteLine("-------------------------------------");
        Console.WriteLine($"⚙️  Mode: {mode}");
        Console.WriteLine($"📂 Files scanned:      {summary.FilesScanned}");
        Console.WriteLine($"📝 Files changed:      {summary.FilesChanged}");
        Console.WriteLine($"🔎 Candidates found:   {summary.CandidatesFound}");
        Console.WriteLine($"✅ Candidates updated: {summary.CandidatesUpdated}");
        Console.WriteLine($"⏱️  Elapsed:           {elapsed.TotalMilliseconds:N0} ms ({elapsed.TotalSeconds:F2} s)");

        if (mode == MigrationHideSpawnMenuMode.Check)
        {
            var status = summary.Violations.Count == 0 ? "✅ OK" : "❌ OUT OF SYNC";
            Console.WriteLine($"🧪 Check status:      {status}");
            Console.WriteLine($"⚠️  Violations:       {summary.Violations.Count}");
        }
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  dotnet run --project Content.MigrationHideSpawnMenu -- sync");
        Console.WriteLine("  dotnet run --project Content.MigrationHideSpawnMenu -- check");
        Console.WriteLine("  dotnet run --project Content.MigrationHideSpawnMenu -- hide-spawn-menu sync");
        Console.WriteLine("  dotnet run --project Content.MigrationHideSpawnMenu -- hide-spawn-menu check");
    }

    private readonly record struct ConstructionGraphNodeKey(string GraphId, string NodeId)
    {
        public string GraphId { get; } = GraphId;
        public string NodeId { get; } = NodeId;
    }

    private sealed class PendingCandidate(int startLineIndex, string violationId, Action applyChange)
    {
        public int StartLineIndex { get; } = startLineIndex;
        public string ViolationId { get; } = violationId;
        public Action ApplyChange { get; } = applyChange;
    }
}
