using System;
using System.Collections.Generic;
using System.IO;
using Content.MigrationHideSpawnMenu;
using NUnit.Framework;

namespace Content.Tests._Scp.Migration;

[TestFixture]
[NonParallelizable]
public sealed class MigrationHideSpawnMenuSyncToolTests
{
    private const string EditComment = "Unit test edit";
    private const string DefaultPrototypeRelativePath = "Resources/Prototypes/Entities/test.yml";
    private const string ConstructionGraphRelativePath = "Resources/Prototypes/Recipes/Construction/Graphs/test_graphs.yml";
    private const string ConstructionRecipeRelativePath = "Resources/Prototypes/Recipes/Construction/test_recipes.yml";
    private const string ConstructionGraphContent = """
        - type: constructionGraph
          id: OldGraph
          start: start
          graph:
            - node: old
              entity: OldEntity
            - node: oldAlt
              entity: OldEntity

        - type: constructionGraph
          id: NewGraph
          start: start
          graph:
          - node: new
            entity: NewEntity

        - type: constructionGraph
          id: DynamicGraph
          start: start
          graph:
          - node: dynamic
            entity: !type:BoardNodeEntity { container: board }
        """;

    private readonly List<string> _tempRepositories = [];

    [TearDown]
    public void TearDown()
    {
        foreach (var path in _tempRepositories)
        {
            if (!Directory.Exists(path))
                continue;

            Directory.Delete(path, true);
        }

        _tempRepositories.Clear();
    }

    [Test]
    public void SyncAddsHideSpawnMenuToInlineCategories()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
              categories: [ Debug ]
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Contain("categories: [ Debug, HideSpawnMenu ] # Unit test edit"));
    }

    [Test]
    public void SyncAddsHideSpawnMenuToBlockCategories()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
              categories:
              - Debug
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Contain("  - Debug"));
        Assert.That(updated, Does.Contain("  - HideSpawnMenu # Unit test edit"));
    }

    [Test]
    public void SyncCreatesCategoriesWhenMissing()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
              components:
              - type: Transform
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Contain("  categories: [ HideSpawnMenu ] # Unit test edit"));
    }

    [Test]
    public void SyncAddsHideSpawnMenuWhenMigrationTargetIsNull()
    {
        var repoRoot = CreateRepository(
            "OldEntity: null\n",
            """
            - type: entity
              id: OldEntity
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Contain("  categories: [ HideSpawnMenu ] # Unit test edit"));
    }

    [Test]
    public void SyncAddsHideSpawnMenuWhenMigrationTargetIsEmpty()
    {
        var repoRoot = CreateRepository(
            "OldEntity:\n",
            """
            - type: entity
              id: OldEntity
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Contain("  categories: [ HideSpawnMenu ] # Unit test edit"));
    }

    [Test]
    public void SyncSkipsAbstractPrototypes()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
              abstract: true
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Not.Contain("HideSpawnMenu"));
    }

    [Test]
    public void SyncDoesNotDuplicateHideSpawnMenu()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
              categories: [ Debug, HideSpawnMenu ]
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        var updated = ReadPrototype(repoRoot);
        Assert.That(CountOccurrences(updated, "HideSpawnMenu"), Is.EqualTo(1));
    }

    [Test]
    public void CheckReturnsFailureWhenMissingHideSpawnMenu()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
            """);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "check" ], repoRoot, EditComment);
        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.CheckOutOfSyncExitCode));
    }

    [TestCaseSource(nameof(GetConstructionSyncScenarios))]
    public void SyncHandlesConstructionRecipeScenarios(ConstructionSyncScenario scenario)
    {
        var repoRoot = CreateRepository(scenario.MigrationContent, scenario.Files);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));
        Assert.Multiple(() =>
        {
            foreach (var assertion in scenario.Assertions)
            {
                var content = ReadRepositoryFile(repoRoot, assertion.RelativePath);

                if (assertion.ExpectedOccurrences.HasValue)
                {
                    Assert.That(
                        CountOccurrences(content, assertion.Value),
                        Is.EqualTo(assertion.ExpectedOccurrences.Value),
                        assertion.RelativePath);
                    continue;
                }

                if (assertion.ShouldContain)
                {
                    Assert.That(content, Does.Contain(assertion.Value), assertion.RelativePath);
                    continue;
                }

                Assert.That(content, Does.Not.Contain(assertion.Value), assertion.RelativePath);
            }
        });
    }

    [Test]
    public void CheckReturnsFailureWhenLegacyConstructionRecipeIsVisible()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            ConstructionFiles(BuildConstructionRecipeFileContent(
                ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))));

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "check" ], repoRoot, EditComment);

        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.CheckOutOfSyncExitCode));
    }

    [Test]
    public void SyncUsesFallbackEditCommentWhenEnvAndOverrideMissing()
    {
        var previous = Environment.GetEnvironmentVariable(MigrationHideSpawnMenuSyncTool.EditCommentEnvironmentVariable);
        Environment.SetEnvironmentVariable(MigrationHideSpawnMenuSyncTool.EditCommentEnvironmentVariable, null);
        try
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                """
                - type: entity
                  id: OldEntity
                """);

            var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, null);
            Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));

            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Contain("# Fire edit"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MigrationHideSpawnMenuSyncTool.EditCommentEnvironmentVariable, previous);
        }
    }

    [Test]
    public void SyncUsesEnvironmentEditCommentWhenOverrideMissing()
    {
        var previous = Environment.GetEnvironmentVariable(MigrationHideSpawnMenuSyncTool.EditCommentEnvironmentVariable);
        Environment.SetEnvironmentVariable(MigrationHideSpawnMenuSyncTool.EditCommentEnvironmentVariable, "CI comment");
        try
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                """
                - type: entity
                  id: OldEntity
                """);

            var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], repoRoot, null);
            Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));

            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Contain("# CI comment"));
        }
        finally
        {
            Environment.SetEnvironmentVariable(MigrationHideSpawnMenuSyncTool.EditCommentEnvironmentVariable, previous);
        }
    }

    [Test]
    public void SyncResolvesRepositoryRootFromNestedDirectory()
    {
        var repoRoot = CreateRepository(
            "OldEntity: NewEntity\n",
            """
            - type: entity
              id: OldEntity
            """);
        var nestedDirectory = Path.Combine(repoRoot, "bin", "Content.MigrationHideSpawnMenu");
        Directory.CreateDirectory(nestedDirectory);

        var exitCode = MigrationHideSpawnMenuSyncTool.Run([ "sync" ], nestedDirectory, EditComment);
        Assert.That(exitCode, Is.EqualTo(MigrationHideSpawnMenuSyncTool.SuccessExitCode));

        var updated = ReadPrototype(repoRoot);
        Assert.That(updated, Does.Contain("categories: [ HideSpawnMenu ] # Unit test edit"));
    }

    private static IEnumerable<TestCaseData> GetConstructionSyncScenarios()
    {
        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "hide: true # Unit test edit"),
                ]))
            .SetName("SyncHidesLegacyConstructionRecipeWhenVisibleReplacementExists");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old", "false"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "hide: true # Unit test edit"),
                    new FileAssertion(ConstructionRecipeRelativePath, "hide: false", false),
                ]))
            .SetName("SyncRewritesHideFalseForLegacyConstructionRecipe");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old", "true # existing hide"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "hide: true # existing hide", ExpectedOccurrences: 1),
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncLeavesAlreadyHiddenLegacyConstructionRecipeUnchanged");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new", "true"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncSkipsLegacyConstructionRecipeWhenReplacementIsHidden");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncSkipsLegacyConstructionRecipeWhenReplacementDoesNotExist");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: null\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncSkipsLegacyConstructionRecipeWhenMigrationTargetIsNull");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity:\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncSkipsLegacyConstructionRecipeWhenMigrationTargetIsEmpty");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                    ConstructionRecipeBlock("OldAltRecipe", "OldGraph", "oldAlt"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "hide: true # Unit test edit", ExpectedOccurrences: 2),
                ]))
            .SetName("SyncHidesAllLegacyConstructionRecipesForSameMigratedEntity");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "old"),
                    ConstructionRecipeBlock("DynamicReplacementRecipe", "DynamicGraph", "dynamic"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncIgnoresDynamicReplacementConstructionRecipes");

        yield return new TestCaseData(new ConstructionSyncScenario(
                "OldEntity: NewEntity\n",
                ConstructionFiles(BuildConstructionRecipeFileContent(
                    ConstructionRecipeBlock("OldRecipe", "OldGraph", "missingTarget"),
                    ConstructionRecipeBlock("NewRecipe", "NewGraph", "new"))),
                [
                    new FileAssertion(ConstructionRecipeRelativePath, "# Unit test edit", false),
                ]))
            .SetName("SyncIgnoresLegacyConstructionRecipesWithUnresolvedTargetNode");
    }

    private string CreateRepository(string migrationContent, string prototypeContent)
    {
        return CreateRepository(
            migrationContent,
            [ new RepositoryFile(DefaultPrototypeRelativePath, prototypeContent) ]);
    }

    private string CreateRepository(string migrationContent, params RepositoryFile[] files)
    {
        var root = Path.Combine(Path.GetTempPath(), $"hide_spawn_menu_sync_{Guid.NewGuid():N}");
        _tempRepositories.Add(root);

        var migrationPath = Path.Combine(root, "Resources", "migration.yml");
        Directory.CreateDirectory(Path.GetDirectoryName(migrationPath)!);
        File.WriteAllText(migrationPath, NormalizeMultiline(migrationContent));

        foreach (var file in files)
        {
            var filePath = Path.Combine(root, file.RelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            File.WriteAllText(filePath, NormalizeMultiline(file.Content));
        }

        return root;
    }

    private static RepositoryFile[] ConstructionFiles(string recipeContent)
    {
        return
        [
            new RepositoryFile(ConstructionGraphRelativePath, ConstructionGraphContent),
            new RepositoryFile(ConstructionRecipeRelativePath, recipeContent),
        ];
    }

    private static string BuildConstructionRecipeFileContent(params string[] blocks)
    {
        return string.Join("\n\n", blocks);
    }

    private static string ConstructionRecipeBlock(string id, string graphId, string targetNode, string hideValue = null)
    {
        var lines = new List<string>
        {
            "- type: construction",
            $"  id: {id}",
            $"  graph: {graphId}",
            "  startNode: start",
            $"  targetNode: {targetNode}",
        };

        if (hideValue != null)
            lines.Add($"  hide: {hideValue}");

        lines.Add("  category: construction-category-test");
        lines.Add("  objectType: Item");
        return string.Join("\n", lines);
    }

    private static string ReadPrototype(string root)
    {
        return ReadRepositoryFile(root, DefaultPrototypeRelativePath);
    }

    private static string ReadRepositoryFile(string root, string relativePath)
    {
        return File.ReadAllText(Path.Combine(root, relativePath));
    }

    private static string NormalizeMultiline(string content)
    {
        content = content.Replace("\r\n", "\n");
        if (content.StartsWith('\n'))
            content = content.Substring(1);
        return content;
    }

    private static int CountOccurrences(string input, string value)
    {
        return input.Split(value).Length - 1;
    }

    public sealed record RepositoryFile(string RelativePath, string Content);

    public sealed record FileAssertion(
        string RelativePath,
        string Value,
        bool ShouldContain = true,
        int? ExpectedOccurrences = null);

    public sealed record ConstructionSyncScenario(
        string MigrationContent,
        RepositoryFile[] Files,
        FileAssertion[] Assertions);
}
