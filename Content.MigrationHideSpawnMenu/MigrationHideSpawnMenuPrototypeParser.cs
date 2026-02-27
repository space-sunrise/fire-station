using System;
using System.Collections.Generic;
using System.Linq;

namespace Content.MigrationHideSpawnMenu;

internal static class MigrationHideSpawnMenuPrototypeParser
{
    private const string EntityType = "entity";
    private const string IdField = "id";
    private const string AbstractField = "abstract";
    private const string CategoriesField = "categories";

    public static MigrationHideSpawnMenuPrototypeFile Parse(string content)
    {
        var newLine = content.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        var normalized = content.Replace("\r\n", "\n");
        var endsWithTrailingNewLine = normalized.EndsWith('\n');
        var lines = normalized.Split('\n').ToList();
        if (endsWithTrailingNewLine && lines.Count > 0 && lines[^1].Length == 0)
            lines.RemoveAt(lines.Count - 1);

        var result = new MigrationHideSpawnMenuPrototypeFile(newLine, endsWithTrailingNewLine, lines);

        var entries = FindTopLevelPrototypeEntries(lines);
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!entry.PrototypeType.Equals(EntityType, StringComparison.Ordinal))
                continue;

            var endExclusive = i + 1 < entries.Count ? entries[i + 1].LineIndex : lines.Count;
            var block = ParseEntityBlock(lines, entry.LineIndex, endExclusive, entry.Indent);
            result.EntityBlocks.Add(block);
        }

        return result;
    }

    public static string ComposeContent(MigrationHideSpawnMenuPrototypeFile file)
    {
        var content = string.Join(file.NewLine, file.Lines);
        if (file.EndsWithTrailingNewLine)
            content += file.NewLine;

        return content;
    }

    private static List<PrototypeEntry> FindTopLevelPrototypeEntries(List<string> lines)
    {
        var entries = new List<PrototypeEntry>();
        int? minIndent = null;

        for (var i = 0; i < lines.Count; i++)
        {
            if (!TryParsePrototypeType(lines[i], out var indent, out var prototypeType))
                continue;

            if (!minIndent.HasValue || indent < minIndent.Value)
                minIndent = indent;

            entries.Add(new PrototypeEntry(i, indent, prototypeType));
        }

        if (!minIndent.HasValue)
            return new List<PrototypeEntry>();

        return entries.Where(e => e.Indent == minIndent.Value).ToList();
    }

    private static MigrationHideSpawnMenuEntityBlock ParseEntityBlock(List<string> lines, int startLineIndex, int endLineExclusive, int prototypeIndent)
    {
        var block = new MigrationHideSpawnMenuEntityBlock
        {
            StartLineIndex = startLineIndex,
            EndLineIndexExclusive = endLineExclusive,
            PrototypeIndent = prototypeIndent,
        };

        var fieldIndent = FindFieldIndent(lines, startLineIndex, endLineExclusive, prototypeIndent);
        block.FieldIndent = fieldIndent;

        if (fieldIndent < 0)
            return block;

        for (var i = startLineIndex + 1; i < endLineExclusive; i++)
        {
            if (TryParseFieldValue(lines[i], fieldIndent, IdField, out var id))
            {
                block.HasId = !string.IsNullOrWhiteSpace(id);
                block.Id = id;
                block.IdLineIndex = i;
                continue;
            }

            if (TryParseFieldValue(lines[i], fieldIndent, AbstractField, out var abstractValue))
            {
                if (abstractValue.Equals("true", StringComparison.OrdinalIgnoreCase))
                    block.IsAbstract = true;
                continue;
            }

            if (!TryParseCategoriesField(lines[i], fieldIndent, out var categoriesRemainder))
                continue;

            block.CategoriesLineIndex = i;
            ParseCategories(lines, i, endLineExclusive, fieldIndent, categoriesRemainder, block);
        }

        return block;
    }

    private static void ParseCategories(
        List<string> lines,
        int categoriesLineIndex,
        int endLineExclusive,
        int fieldIndent,
        string categoriesRemainder,
        MigrationHideSpawnMenuEntityBlock block)
    {
        if (categoriesRemainder.StartsWith('['))
        {
            block.CategoryKind = MigrationHideSpawnMenuCategoryKind.Inline;
            ParseInlineCategories(categoriesRemainder, block.Categories);
            return;
        }

        block.CategoryKind = MigrationHideSpawnMenuCategoryKind.Block;

        for (var i = categoriesLineIndex + 1; i < endLineExclusive; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var indent = CountIndent(line);
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith('#'))
                continue;

            if (indent < fieldIndent)
                break;

            if (indent == fieldIndent && !trimmed.StartsWith('-'))
                break;

            if (!trimmed.StartsWith('-'))
                continue;

            if (block.CategoryItemIndent < 0)
                block.CategoryItemIndent = indent;

            block.CategoryItemLineIndices.Add(i);
            var value = StripInlineComment(trimmed.Substring(1)).Trim();
            value = TrimQuotes(value);
            if (!string.IsNullOrWhiteSpace(value))
                block.Categories.Add(value);
        }
    }

    private static void ParseInlineCategories(string categoriesRemainder, List<string> target)
    {
        var closeIndex = categoriesRemainder.IndexOf(']');
        if (closeIndex < 0)
            return;

        var inner = categoriesRemainder.Substring(1, closeIndex - 1);
        var split = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var entry in split)
        {
            var trimmed = TrimQuotes(entry.Trim());
            if (!string.IsNullOrWhiteSpace(trimmed))
                target.Add(trimmed);
        }
    }

    private static int FindFieldIndent(List<string> lines, int startLineIndex, int endLineExclusive, int prototypeIndent)
    {
        for (var i = startLineIndex + 1; i < endLineExclusive; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var indent = CountIndent(line);
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith('#'))
                continue;

            if (indent <= prototypeIndent)
                continue;

            if (trimmed.StartsWith('-'))
                continue;

            if (trimmed.Contains(':'))
                return indent;
        }

        return -1;
    }

    private static bool TryParsePrototypeType(string line, out int indent, out string prototypeType)
    {
        indent = 0;
        prototypeType = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        indent = CountIndent(line);
        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith('-'))
            return false;

        trimmed = trimmed.Substring(1).TrimStart();
        if (!trimmed.StartsWith("type:", StringComparison.Ordinal))
            return false;

        var value = StripInlineComment(trimmed.Substring("type:".Length)).Trim();
        value = TrimQuotes(value);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        prototypeType = value;
        return true;
    }

    private static bool TryParseFieldValue(string line, int fieldIndent, string fieldName, out string value)
    {
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        var indent = CountIndent(line);
        if (indent != fieldIndent)
            return false;

        var trimmed = line.TrimStart();
        if (trimmed.StartsWith('#'))
            return false;

        if (!trimmed.StartsWith(fieldName, StringComparison.Ordinal))
            return false;

        if (trimmed.Length <= fieldName.Length || trimmed[fieldName.Length] != ':')
            return false;

        value = StripInlineComment(trimmed.Substring(fieldName.Length + 1)).Trim();
        value = TrimQuotes(value);
        return true;
    }

    private static bool TryParseCategoriesField(string line, int fieldIndent, out string remainder)
    {
        remainder = string.Empty;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        var indent = CountIndent(line);
        if (indent != fieldIndent)
            return false;

        var trimmed = line.TrimStart();
        if (!trimmed.StartsWith(CategoriesField, StringComparison.Ordinal))
            return false;

        if (trimmed.Length <= CategoriesField.Length || trimmed[CategoriesField.Length] != ':')
            return false;

        remainder = trimmed.Substring(CategoriesField.Length + 1).Trim();
        return true;
    }

    private static int CountIndent(string line)
    {
        var i = 0;
        while (i < line.Length && (line[i] == ' ' || line[i] == '\t'))
        {
            i++;
        }

        return i;
    }

    private static string StripInlineComment(string value)
    {
        var index = value.IndexOf('#');
        if (index < 0)
            return value;

        return value.Substring(0, index);
    }

    private static string TrimQuotes(string value)
    {
        if (value.Length >= 2)
        {
            if ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\''))
                return value.Substring(1, value.Length - 2);
        }

        return value;
    }

    private readonly record struct PrototypeEntry(int LineIndex, int Indent, string PrototypeType)
    {
        public int LineIndex { get; } = LineIndex;
        public int Indent { get; } = Indent;
        public string PrototypeType { get; } = PrototypeType;
    }
}
