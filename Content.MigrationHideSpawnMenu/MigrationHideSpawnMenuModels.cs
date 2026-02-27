using System.Collections.Generic;

namespace Content.MigrationHideSpawnMenu
{
    internal enum MigrationHideSpawnMenuMode
    {
        Sync,
        Check
    }

    internal enum MigrationHideSpawnMenuCategoryKind
    {
        Missing,
        Inline,
        Block
    }

    internal sealed class MigrationHideSpawnMenuEntityBlock
    {
        public int StartLineIndex { get; set; }
        public int EndLineIndexExclusive { get; set; }
        public int PrototypeIndent { get; set; }
        public int FieldIndent { get; set; } = -1;

        public bool HasId { get; set; }
        public int IdLineIndex { get; set; } = -1;
        public string Id { get; set; } = string.Empty;

        public bool IsAbstract { get; set; }

        public MigrationHideSpawnMenuCategoryKind CategoryKind { get; set; } = MigrationHideSpawnMenuCategoryKind.Missing;
        public int CategoriesLineIndex { get; set; } = -1;
        public List<string> Categories { get; } = new();
        public List<int> CategoryItemLineIndices { get; } = new();
        public int CategoryItemIndent { get; set; } = -1;
    }

    internal sealed class MigrationHideSpawnMenuPrototypeFile
    {
        public MigrationHideSpawnMenuPrototypeFile(string filePath, string newLine, bool endsWithTrailingNewLine, List<string> lines)
        {
            FilePath = filePath;
            NewLine = newLine;
            EndsWithTrailingNewLine = endsWithTrailingNewLine;
            Lines = lines;
        }

        public string FilePath { get; }
        public string NewLine { get; }
        public bool EndsWithTrailingNewLine { get; }
        public List<string> Lines { get; }
        public List<MigrationHideSpawnMenuEntityBlock> EntityBlocks { get; } = new();
    }

    internal sealed class MigrationHideSpawnMenuSummary
    {
        public int FilesScanned { get; set; }
        public int FilesChanged { get; set; }
        public int CandidatesFound { get; set; }
        public int CandidatesUpdated { get; set; }
        public List<string> Violations { get; } = new();
    }
}
