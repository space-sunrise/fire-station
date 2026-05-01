using System.Collections.Generic;
using Content.Shared._Scp.Knowledge;
using NUnit.Framework;

namespace Content.Tests.Shared._Scp.Knowledge;

[TestFixture]
public sealed class ScpKnowledgeTextTest
{
    [Test]
    public void AlternativesMatchAnyOption()
    {
        var matches = Match("(scp|сцп) 173", "СЦП-173 был замечен");

        Assert.That(matches.Count, Is.EqualTo(1));
    }

    [Test]
    public void OptionalGroupIsAppliedOnlyWhenPresent()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Match("object[ number] 049", "object 049").Count, Is.EqualTo(1));
            Assert.That(Match("object[ number] 049", "object number 049").Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void NestedGroupsMatchExpectedSequence()
    {
        var matches = Match("(((scp|сцп))|((объект)[ (номер)])) 131", "объект номер 131");

        Assert.That(matches.Count, Is.EqualTo(1));
    }

    [Test]
    public void EscapedMetacharactersAreParsedAsLiteralText()
    {
        var matches = Match("\\(object\\|number\\) 049", "(object|number) 049");

        Assert.That(matches.Count, Is.EqualTo(1));
    }

    [Test]
    public void SeparatorVariantsMatchTheSamePattern()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Match("scp 939", "scp-939").Count, Is.EqualTo(1));
            Assert.That(Match("scp 939", "scp 939").Count, Is.EqualTo(1));
        });
    }

    [Test]
    public void ExplicitFillerWordsMustBeAuthored()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Match("object[ number] 049", "object number 049").Count, Is.EqualTo(1));
            Assert.That(Match("object[ number] 049", "object many random words 049").Count, Is.EqualTo(0));
        });
    }

    [Test]
    public void MatchSpanCoversOriginalSourceText()
    {
        const string text = "Внимание: объект номер 049 уже здесь.";
        var matches = Match("object[ number] 049|(объект)[ (номер)] 049", text);

        Assert.That(matches.Count, Is.EqualTo(1));
        Assert.That(text[matches[0].SourceStart..matches[0].SourceEnd], Is.EqualTo("объект номер 049"));
    }

    [Test]
    public void RussianAndEnglishPatternsBothWorkWhenResolved()
    {
        Assert.Multiple(() =>
        {
            Assert.That(Match("(scp|сцп) 131", "СЦП 131").Count, Is.EqualTo(1));
            Assert.That(Match("object[ number] 049", "object number 049").Count, Is.EqualTo(1));
        });
    }

    private static List<ScpKnowledgeTextMatch> Match(string patternText, string text)
    {
        Assert.That(
            ScpKnowledgeText.TryCompileRecognitionPattern(patternText, out var pattern, out var error),
            Is.True,
            error);

        var matches = new List<ScpKnowledgeTextMatch>();
        ScpKnowledgeText.AddPatternMatches(pattern!, ScpKnowledgeText.TokenizeRecognitionText(text), matches);
        return matches;
    }
}
