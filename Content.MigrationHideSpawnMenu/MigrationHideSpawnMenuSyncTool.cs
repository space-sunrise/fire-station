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

    private const int CheckOutOfSyncExitCode = 1;
    private const int TechnicalFailureExitCode = 2;

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

            if (mode == MigrationHideSpawnMenuMode.Check && summary.Violations.Count > 0)
            {
                foreach (var violation in summary.Violations)
                {
                    Console.WriteLine(violation);
                }

                return CheckOutOfSyncExitCode;
            }

            return 0;
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

        var sourceIds = ReadMigrationSourceIds(migrationPath);
        var summary = new MigrationHideSpawnMenuSummary();

        foreach (var prototypePath in Directory.EnumerateFiles(prototypesRoot, "*.yml", SearchOption.AllDirectories))
        {
            summary.FilesScanned++;

            var originalContent = File.ReadAllText(prototypePath);
            var parsed = MigrationHideSpawnMenuPrototypeParser.Parse(originalContent);

            var changedInFile = false;
            var blocks = parsed.EntityBlocks.OrderByDescending(static b => b.StartLineIndex);
            foreach (var block in blocks)
            {
                if (!IsCandidate(sourceIds, block))
                    continue;

                summary.CandidatesFound++;

                if (mode == MigrationHideSpawnMenuMode.Check)
                {
                    summary.Violations.Add($"{Path.GetRelativePath(repositoryRoot, prototypePath)}: {block.Id}");
                    continue;
                }

                AddHideSpawnMenuCategory(parsed.Lines, block, editComment);
                summary.CandidatesUpdated++;
                changedInFile = true;
            }

            if (mode != MigrationHideSpawnMenuMode.Sync || !changedInFile)
                continue;

            var updatedContent = MigrationHideSpawnMenuPrototypeParser.ComposeContent(parsed);
            if (string.Equals(updatedContent, originalContent, StringComparison.Ordinal))
                continue;

            File.WriteAllText(prototypePath, updatedContent);
            summary.FilesChanged++;
        }

        return summary;
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

    private static bool IsCandidate(HashSet<string> sourceIds, MigrationHideSpawnMenuEntityBlock block)
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

    private static HashSet<string> ReadMigrationSourceIds(string migrationPath)
    {
        using var reader = new StreamReader(migrationPath);
        var yaml = new YamlStream();
        yaml.Load(reader);

        var result = new HashSet<string>(StringComparer.Ordinal);
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
                    continue;

                result.Add(oldId);
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
}
