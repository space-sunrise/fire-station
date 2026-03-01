using System.Collections.Generic;

namespace Content.MigrationHideSpawnMenu;

internal enum MigrationHideSpawnMenuMode
{
    Sync,
    Check,
}

internal enum MigrationHideSpawnMenuCategoryKind
{
    Missing,
    Inline,
    Block,
}

internal sealed class MigrationHideSpawnMenuEntityBlock
{
    public int StartLineIndex { get; init; }
    public int EndLineIndexExclusive { get; set; }
    public int PrototypeIndent { get; init; }
    public int FieldIndent { get; set; } = -1;

    public bool HasId { get; set; }
    public int IdLineIndex { get; set; } = -1;
    public string Id { get; set; } = string.Empty;

    public bool IsAbstract { get; set; }

    public MigrationHideSpawnMenuCategoryKind CategoryKind { get; set; } = MigrationHideSpawnMenuCategoryKind.Missing;
    public int CategoriesLineIndex { get; set; } = -1;
    public List<string> Categories { get; } = [];
    public List<int> CategoryItemLineIndices { get; } = [];
    public int CategoryItemIndent { get; set; } = -1;
}

internal sealed class MigrationHideSpawnMenuPrototypeFile(string newLine, bool endsWithTrailingNewLine, List<string> lines)
{
    public string NewLine { get; } = newLine;
    public bool EndsWithTrailingNewLine { get; } = endsWithTrailingNewLine;
    public List<string> Lines { get; } = lines;
    public List<MigrationHideSpawnMenuEntityBlock> EntityBlocks { get; } = [];
}

internal sealed class MigrationHideSpawnMenuSummary
{
    public int FilesScanned { get; set; }
    public int FilesChanged { get; set; }
    public int CandidatesFound { get; set; }
    public int CandidatesUpdated { get; set; }
    public List<string> Violations { get; } = [];
}
