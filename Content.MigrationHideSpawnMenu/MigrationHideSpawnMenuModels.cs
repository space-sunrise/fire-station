using System;
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

internal sealed class MigrationHideSpawnMenuConstructionBlock
{
    public int StartLineIndex { get; init; }
    public int EndLineIndexExclusive { get; set; }
    public int PrototypeIndent { get; init; }
    public int FieldIndent { get; set; } = -1;

    public bool HasId { get; set; }
    public int IdLineIndex { get; set; } = -1;
    public string Id { get; set; } = string.Empty;

    public bool HasGraph { get; set; }
    public int GraphLineIndex { get; set; } = -1;
    public string GraphId { get; set; } = string.Empty;

    public bool HasTargetNode { get; set; }
    public int TargetNodeLineIndex { get; set; } = -1;
    public string TargetNode { get; set; } = string.Empty;

    public bool HasHide { get; set; }
    public int HideLineIndex { get; set; } = -1;
    public bool Hide { get; set; }
}

internal sealed class MigrationHideSpawnMenuConstructionGraphBlock
{
    public int StartLineIndex { get; init; }
    public int EndLineIndexExclusive { get; set; }
    public int PrototypeIndent { get; init; }
    public int FieldIndent { get; set; } = -1;

    public bool HasId { get; set; }
    public int IdLineIndex { get; set; } = -1;
    public string Id { get; set; } = string.Empty;

    public int GraphLineIndex { get; set; } = -1;
    public Dictionary<string, string> NodeEntities { get; } = new(StringComparer.Ordinal);
}

internal sealed class MigrationHideSpawnMenuPrototypeFile(string newLine, bool endsWithTrailingNewLine, List<string> lines)
{
    public string NewLine { get; } = newLine;
    public bool EndsWithTrailingNewLine { get; } = endsWithTrailingNewLine;
    public List<string> Lines { get; } = lines;
    public List<MigrationHideSpawnMenuEntityBlock> EntityBlocks { get; } = [];
    public List<MigrationHideSpawnMenuConstructionBlock> ConstructionBlocks { get; } = [];
    public List<MigrationHideSpawnMenuConstructionGraphBlock> ConstructionGraphBlocks { get; } = [];
}

internal sealed class MigrationHideSpawnMenuSummary
{
    public int FilesScanned { get; set; }
    public int FilesChanged { get; set; }
    public int CandidatesFound { get; set; }
    public int CandidatesUpdated { get; set; }
    public List<string> Violations { get; } = [];
}

internal sealed class MigrationHideSpawnMenuRepositoryFile(
    string path,
    string originalContent,
    MigrationHideSpawnMenuPrototypeFile parsed)
{
    public string Path { get; } = path;
    public string OriginalContent { get; } = originalContent;
    public MigrationHideSpawnMenuPrototypeFile Parsed { get; } = parsed;
}

internal sealed class MigrationHideSpawnMenuResolvedConstructionBlock(
    MigrationHideSpawnMenuRepositoryFile file,
    MigrationHideSpawnMenuConstructionBlock block,
    string resultEntityId)
{
    public MigrationHideSpawnMenuRepositoryFile File { get; } = file;
    public MigrationHideSpawnMenuConstructionBlock Block { get; } = block;
    public string ResultEntityId { get; } = resultEntityId;
}
