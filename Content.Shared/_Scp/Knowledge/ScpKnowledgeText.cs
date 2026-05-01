using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Content.Shared._Scp.Knowledge;

public static class ScpKnowledgeText
{
    public static string NormalizeRecognitionText(string text)
    {
        return TokenizeRecognitionText(text).NormalizedText;
    }

    public static ScpKnowledgeTokenizedText TokenizeRecognitionText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return ScpKnowledgeTokenizedText.Empty;

        List<ScpKnowledgeTextToken> tokens = [];
        var tokenBuilder = new StringBuilder(text.Length);
        var normalizedBuilder = new StringBuilder(text.Length);
        var tokenStart = -1;

        for (var i = 0; i < text.Length; i++)
        {
            if (TryConsumeMarkupTag(text, ref i))
                continue;

            var character = NormalizeRecognitionCharacter(text[i]);
            if (char.IsLetterOrDigit(character))
            {
                if (tokenStart == -1)
                    tokenStart = i;

                tokenBuilder.Append(character);
                continue;
            }

            FlushToken(tokens, tokenBuilder, normalizedBuilder, ref tokenStart, i);
        }

        FlushToken(tokens, tokenBuilder, normalizedBuilder, ref tokenStart, text.Length);

        return tokens.Count == 0
            ? ScpKnowledgeTokenizedText.Empty
            : new ScpKnowledgeTokenizedText(text, normalizedBuilder.ToString(), tokens);
    }

    public static bool TryCompileRecognitionPattern(
        string pattern,
        [NotNullWhen(true)] out ScpKnowledgeCompiledPattern? compiledPattern,
        [NotNullWhen(false)] out string? error)
    {
        compiledPattern = null;

        if (string.IsNullOrWhiteSpace(pattern))
        {
            error = "Pattern is empty.";
            return false;
        }

        try
        {
            var parser = new ScpKnowledgePatternParser(pattern);
            var root = parser.ParsePattern();

            if (root.MinTokenCount <= 0)
            {
                error = "Pattern does not contain any recognition tokens.";
                return false;
            }

            HashSet<string> firstTokens = [];
            root.CollectFirstTokens(firstTokens);
            if (firstTokens.Count == 0)
            {
                error = "Pattern does not contain any recognition start tokens.";
                return false;
            }

            var firstTokensArray = new string[firstTokens.Count];
            firstTokens.CopyTo(firstTokensArray);
            compiledPattern = new ScpKnowledgeCompiledPattern(pattern, root, firstTokensArray);
            error = null;
            return true;
        }
        catch (ScpKnowledgePatternParseException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    public static void AddPatternMatches(
        ScpKnowledgeCompiledPattern pattern,
        ScpKnowledgeTokenizedText tokenizedText,
        List<ScpKnowledgeTextMatch> matches)
    {
        if (pattern.MinTokenCount <= 0 || tokenizedText.Tokens.Count < pattern.MinTokenCount)
            return;

        var context = new ScpKnowledgePatternMatchContext();
        for (var tokenIndex = 0; tokenIndex <= tokenizedText.Tokens.Count - pattern.MinTokenCount; tokenIndex++)
        {
            AddPatternMatchesAt(pattern, tokenizedText, tokenIndex, matches, context);
        }
    }

    public static void AddPatternMatchesAt(
        ScpKnowledgeCompiledPattern pattern,
        ScpKnowledgeTokenizedText tokenizedText,
        int tokenIndex,
        List<ScpKnowledgeTextMatch> matches,
        ScpKnowledgePatternMatchContext? context = null)
    {
        if (pattern.MinTokenCount <= 0 ||
            tokenIndex < 0 ||
            tokenIndex + pattern.MinTokenCount > tokenizedText.Tokens.Count)
        {
            return;
        }

        context ??= new ScpKnowledgePatternMatchContext();
        context.Reset();

        var endBuffer = context.RentIndicesBuffer();
        pattern.Root.CollectMatchEnds(tokenizedText.Tokens, tokenIndex, endBuffer, context);

        foreach (var endIndex in endBuffer)
        {
            if (endIndex <= tokenIndex)
                continue;

            var startToken = tokenizedText.Tokens[tokenIndex];
            var endToken = tokenizedText.Tokens[endIndex - 1];
            AddUniqueMatch(matches,
                new ScpKnowledgeTextMatch(
                    tokenIndex,
                    endIndex,
                    startToken.SourceStart,
                    endToken.SourceEnd));
        }
    }

    private static char NormalizeRecognitionCharacter(char character)
    {
        var normalized = char.ToLowerInvariant(character);
        return normalized == 'ё' ? 'е' : normalized;
    }

    private static void FlushToken(
        List<ScpKnowledgeTextToken> tokens,
        StringBuilder tokenBuilder,
        StringBuilder normalizedBuilder,
        ref int tokenStart,
        int tokenEnd)
    {
        if (tokenBuilder.Length == 0 || tokenStart == -1)
            return;

        var token = tokenBuilder.ToString();
        if (normalizedBuilder.Length > 0)
            normalizedBuilder.Append(' ');

        normalizedBuilder.Append(token);
        tokens.Add(new ScpKnowledgeTextToken(token, tokenStart, tokenEnd));
        tokenBuilder.Clear();
        tokenStart = -1;
    }

    private static bool TryConsumeMarkupTag(string text, ref int index)
    {
        if (text[index] != '[')
            return false;

        var current = index + 1;
        if (current >= text.Length)
            return false;

        if (text[current] == '/')
            current++;

        while (current < text.Length && char.IsWhiteSpace(text[current]))
        {
            current++;
        }

        if (current >= text.Length || !char.IsLetter(text[current]))
            return false;

        while (current < text.Length)
        {
            if (text[current] == '\\')
            {
                current = Math.Min(current + 2, text.Length);
                continue;
            }

            if (text[current] == ']')
            {
                index = current;
                return true;
            }

            current++;
        }

        return false;
    }

    private static void AddUniqueMatch(List<ScpKnowledgeTextMatch> matches, ScpKnowledgeTextMatch match)
    {
        foreach (var existingMatch in matches)
        {
            if (existingMatch == match)
                return;
        }

        matches.Add(match);
    }

    private static void AddUniqueIndex(List<int> indices, int index)
    {
        foreach (var existingIndex in indices)
        {
            if (existingIndex == index)
                return;
        }

        indices.Add(index);
    }

    private sealed class ScpKnowledgePatternParser(string pattern)
    {
        private int _index;

        public ScpKnowledgePatternNode ParsePattern()
        {
            var node = ParseExpression();
            if (_index != pattern.Length)
                throw Error($"Unexpected trailing character '{pattern[_index]}' at index {_index}.");

            return node;
        }

        private ScpKnowledgePatternNode ParseExpression(char? terminator = null)
        {
            List<ScpKnowledgePatternNode> options = [];
            var sawAlternative = false;

            while (true)
            {
                var option = ParseSequence(terminator);
                options.Add(option);

                if (_index >= pattern.Length || pattern[_index] != '|')
                    break;

                sawAlternative = true;
                _index++;

                if (_index >= pattern.Length || (terminator != null && pattern[_index] == terminator))
                    throw Error("Alternative group contains an empty option.");
            }

            if (!sawAlternative)
                return options[0];

            foreach (var optionNode in options)
            {
                if (optionNode.CanMatchEmpty)
                    throw Error("Alternative group contains an empty option.");
            }

            return new ScpKnowledgeAlternativePatternNode(options);
        }

        private ScpKnowledgePatternNode ParseSequence(char? terminator)
        {
            List<ScpKnowledgePatternNode> children = [];
            var literal = new StringBuilder();

            while (_index < pattern.Length)
            {
                var character = pattern[_index];

                if (character == '\\')
                {
                    _index++;
                    if (_index >= pattern.Length)
                        throw Error("Pattern ends with an escape character.");

                    literal.Append(pattern[_index]);
                    _index++;
                    continue;
                }

                if (terminator != null && character == terminator)
                    break;

                if (character == '|')
                    break;

                switch (character)
                {
                    case '(':
                        FlushLiteral(children, literal);
                        _index++;
                        var group = ParseExpression(')');
                        ExpectTerminator(')');
                        AddNode(children, group);
                        continue;
                    case '[':
                        FlushLiteral(children, literal);
                        _index++;
                        var optional = ParseExpression(']');
                        ExpectTerminator(']');
                        AddNode(children, new ScpKnowledgeOptionalPatternNode(optional));
                        continue;
                    case ')':
                    case ']':
                        throw Error($"Unexpected closing character '{character}' at index {_index}.");
                    default:
                        literal.Append(character);
                        _index++;
                        continue;
                }
            }

            FlushLiteral(children, literal);

            return children.Count switch
            {
                0 => ScpKnowledgeEmptyPatternNode.Instance,
                1 => children[0],
                _ => new ScpKnowledgeSequencePatternNode(children),
            };
        }

        private void ExpectTerminator(char terminator)
        {
            if (_index >= pattern.Length || pattern[_index] != terminator)
                throw Error($"Expected closing '{terminator}'.");

            _index++;
        }

        private void FlushLiteral(List<ScpKnowledgePatternNode> children, StringBuilder literal)
        {
            if (literal.Length == 0)
                return;

            var tokens = TokenizeRecognitionText(literal.ToString()).Tokens;
            literal.Clear();

            if (tokens.Count == 0)
                return;

            var tokenValues = new string[tokens.Count];
            var tokenIndex = 0;
            foreach (var token in tokens)
            {
                tokenValues[tokenIndex++] = token.Value;
            }

            children.Add(new ScpKnowledgeLiteralPatternNode(tokenValues));
        }

        private static void AddNode(List<ScpKnowledgePatternNode> children, ScpKnowledgePatternNode node)
        {
            if (node is ScpKnowledgeEmptyPatternNode)
                return;

            children.Add(node);
        }

        private ScpKnowledgePatternParseException Error(string message)
        {
            return new ScpKnowledgePatternParseException($"{message} Pattern: '{pattern}'.");
        }
    }

    internal static void AddUniquePatternIndex(List<int> indices, int index)
    {
        AddUniqueIndex(indices, index);
    }
}

public sealed class ScpKnowledgeTokenizedText(
    string sourceText,
    string normalizedText,
    IReadOnlyList<ScpKnowledgeTextToken> tokens)
{
    public static readonly ScpKnowledgeTokenizedText Empty =
        new(string.Empty, string.Empty, []);

    public string SourceText { get; } = sourceText;
    public string NormalizedText { get; } = normalizedText;
    public IReadOnlyList<ScpKnowledgeTextToken> Tokens { get; } = tokens;
}

public readonly record struct ScpKnowledgeTextToken(string Value, int SourceStart, int SourceEnd)
{
    public int Length => SourceEnd - SourceStart;
}

public readonly record struct ScpKnowledgeTextMatch(int TokenStart, int TokenEnd, int SourceStart, int SourceEnd)
{
    public int Length => SourceEnd - SourceStart;
}

public sealed class ScpKnowledgeCompiledPattern
{
    internal ScpKnowledgePatternNode Root { get; }
    public string SourcePattern { get; }
    public IReadOnlyList<string> FirstTokens { get; }
    public int MinTokenCount => Root.MinTokenCount;

    internal ScpKnowledgeCompiledPattern(string sourcePattern, ScpKnowledgePatternNode root, string[] firstTokens)
    {
        Root = root;
        SourcePattern = sourcePattern;
        FirstTokens = firstTokens;
    }
}

internal abstract class ScpKnowledgePatternNode
{
    public abstract int MinTokenCount { get; }
    public abstract bool CanMatchEmpty { get; }
    public abstract void CollectFirstTokens(HashSet<string> firstTokens);

    public abstract void CollectMatchEnds(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context);
}

internal sealed class ScpKnowledgeEmptyPatternNode : ScpKnowledgePatternNode
{
    public static readonly ScpKnowledgeEmptyPatternNode Instance = new();

    public override int MinTokenCount => 0;
    public override bool CanMatchEmpty => true;
    public override void CollectFirstTokens(HashSet<string> firstTokens)
    {
    }

    public override void CollectMatchEnds(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context)
    {
        ScpKnowledgeText.AddUniquePatternIndex(ends, startIndex);
    }
}

internal sealed class ScpKnowledgeLiteralPatternNode(string[] tokens) : ScpKnowledgePatternNode
{
    private readonly string[] _tokens = tokens;

    public override int MinTokenCount => _tokens.Length;
    public override bool CanMatchEmpty => _tokens.Length == 0;
    public override void CollectFirstTokens(HashSet<string> firstTokens)
    {
        if (_tokens.Length > 0)
            firstTokens.Add(_tokens[0]);
    }

    public override void CollectMatchEnds(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context)
    {
        if (startIndex + _tokens.Length > tokens.Count)
            return;

        var offset = 0;
        foreach (var token in _tokens)
        {
            if (!string.Equals(tokens[startIndex + offset].Value, token, StringComparison.Ordinal))
                return;

            offset++;
        }

        ScpKnowledgeText.AddUniquePatternIndex(ends, startIndex + _tokens.Length);
    }
}

internal sealed class ScpKnowledgeSequencePatternNode(List<ScpKnowledgePatternNode> children) : ScpKnowledgePatternNode
{
    private readonly ScpKnowledgePatternNode[] _children = children.ToArray();

    public override int MinTokenCount
    {
        get
        {
            var total = 0;
            foreach (var child in _children)
            {
                total += child.MinTokenCount;
            }

            return total;
        }
    }

    public override void CollectFirstTokens(HashSet<string> firstTokens)
    {
        foreach (var child in _children)
        {
            child.CollectFirstTokens(firstTokens);
            if (!child.CanMatchEmpty)
                break;
        }
    }

    public override bool CanMatchEmpty
    {
        get
        {
            foreach (var child in _children)
            {
                if (!child.CanMatchEmpty)
                    return false;
            }

            return true;
        }
    }

    public override void CollectMatchEnds(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context)
    {
        MatchChild(tokens, startIndex, 0, ends, context);
    }

    private void MatchChild(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        int childIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context)
    {
        if (childIndex >= _children.Length)
        {
            ScpKnowledgeText.AddUniquePatternIndex(ends, startIndex);
            return;
        }

        if (startIndex + GetRemainingMinTokenCount(childIndex) > tokens.Count)
            return;

        var childEnds = context.RentIndicesBuffer();
        _children[childIndex].CollectMatchEnds(tokens, startIndex, childEnds, context);

        foreach (var childEnd in childEnds)
        {
            MatchChild(tokens, childEnd, childIndex + 1, ends, context);
        }
    }

    private int GetRemainingMinTokenCount(int childIndex)
    {
        var total = 0;
        for (var i = childIndex; i < _children.Length; i++)
        {
            total += _children[i].MinTokenCount;
        }

        return total;
    }
}

internal sealed class ScpKnowledgeAlternativePatternNode(List<ScpKnowledgePatternNode> options) : ScpKnowledgePatternNode
{
    private readonly ScpKnowledgePatternNode[] _options = options.ToArray();

    public override int MinTokenCount
    {
        get
        {
            var min = int.MaxValue;
            foreach (var option in _options)
            {
                min = Math.Min(min, option.MinTokenCount);
            }

            return min == int.MaxValue ? 0 : min;
        }
    }

    public override void CollectFirstTokens(HashSet<string> firstTokens)
    {
        foreach (var option in _options)
        {
            option.CollectFirstTokens(firstTokens);
        }
    }

    public override bool CanMatchEmpty
    {
        get
        {
            foreach (var option in _options)
            {
                if (option.CanMatchEmpty)
                    return true;
            }

            return false;
        }
    }

    public override void CollectMatchEnds(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context)
    {
        foreach (var option in _options)
        {
            option.CollectMatchEnds(tokens, startIndex, ends, context);
        }
    }
}

internal sealed class ScpKnowledgeOptionalPatternNode(ScpKnowledgePatternNode child) : ScpKnowledgePatternNode
{
    private readonly ScpKnowledgePatternNode _child = child;

    public override int MinTokenCount => 0;
    public override bool CanMatchEmpty => true;
    public override void CollectFirstTokens(HashSet<string> firstTokens)
    {
        _child.CollectFirstTokens(firstTokens);
    }

    public override void CollectMatchEnds(
        IReadOnlyList<ScpKnowledgeTextToken> tokens,
        int startIndex,
        List<int> ends,
        ScpKnowledgePatternMatchContext context)
    {
        ScpKnowledgeText.AddUniquePatternIndex(ends, startIndex);
        _child.CollectMatchEnds(tokens, startIndex, ends, context);
    }
}

public sealed class ScpKnowledgePatternMatchContext
{
    private readonly List<List<int>> _indexBuffers = [];
    private int _bufferIndex;

    public void Reset()
    {
        _bufferIndex = 0;
    }

    public List<int> RentIndicesBuffer()
    {
        if (_bufferIndex >= _indexBuffers.Count)
            _indexBuffers.Add([]);

        var buffer = _indexBuffers[_bufferIndex++];
        buffer.Clear();
        return buffer;
    }
}

internal sealed class ScpKnowledgePatternParseException(string message) : Exception(message);
