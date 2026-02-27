using System;
using System.Collections.Generic;
using System.IO;
using Content.MigrationHideSpawnMenu;
using NUnit.Framework;

namespace Content.Tests._Scp.Migration
{
    [TestFixture]
    public sealed class MigrationHideSpawnMenuSyncToolTests
    {
        private const string EditComment = "Unit test edit";
        private readonly List<string> _tempRepositories = new();

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
                @"
- type: entity
  id: OldEntity
  categories: [ Debug ]
");

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, EditComment);

            Assert.That(exitCode, Is.EqualTo(0));
            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Contain("categories: [ Debug, HideSpawnMenu ] # Unit test edit"));
        }

        [Test]
        public void SyncAddsHideSpawnMenuToBlockCategories()
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                @"
- type: entity
  id: OldEntity
  categories:
  - Debug
");

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, EditComment);

            Assert.That(exitCode, Is.EqualTo(0));
            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Contain("  - Debug"));
            Assert.That(updated, Does.Contain("  - HideSpawnMenu # Unit test edit"));
        }

        [Test]
        public void SyncCreatesCategoriesWhenMissing()
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                @"
- type: entity
  id: OldEntity
  components:
  - type: Transform
");

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, EditComment);

            Assert.That(exitCode, Is.EqualTo(0));
            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Contain("  categories: [ HideSpawnMenu ] # Unit test edit"));
        }

        [Test]
        public void SyncSkipsAbstractPrototypes()
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                @"
- type: entity
  id: OldEntity
  abstract: true
");

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, EditComment);

            Assert.That(exitCode, Is.EqualTo(0));
            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Not.Contain("HideSpawnMenu"));
        }

        [Test]
        public void SyncDoesNotDuplicateHideSpawnMenu()
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                @"
- type: entity
  id: OldEntity
  categories: [ Debug, HideSpawnMenu ]
");

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, EditComment);

            Assert.That(exitCode, Is.EqualTo(0));
            var updated = ReadPrototype(repoRoot);
            Assert.That(CountOccurrences(updated, "HideSpawnMenu"), Is.EqualTo(1));
        }

        [Test]
        public void CheckReturnsFailureWhenMissingHideSpawnMenu()
        {
            var repoRoot = CreateRepository(
                "OldEntity: NewEntity\n",
                @"
- type: entity
  id: OldEntity
");

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "check" }, repoRoot, EditComment);
            Assert.That(exitCode, Is.EqualTo(1));
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
                    @"
- type: entity
  id: OldEntity
");

                var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, null);
                Assert.That(exitCode, Is.EqualTo(0));

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
                    @"
- type: entity
  id: OldEntity
");

                var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, repoRoot, null);
                Assert.That(exitCode, Is.EqualTo(0));

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
                @"
- type: entity
  id: OldEntity
");
            var nestedDirectory = Path.Combine(repoRoot, "bin", "Content.MigrationHideSpawnMenu");
            Directory.CreateDirectory(nestedDirectory);

            var exitCode = MigrationHideSpawnMenuSyncTool.Run(new[] { "sync" }, nestedDirectory, EditComment);

            Assert.That(exitCode, Is.EqualTo(0));
            var updated = ReadPrototype(repoRoot);
            Assert.That(updated, Does.Contain("categories: [ HideSpawnMenu ] # Unit test edit"));
        }

        private string CreateRepository(string migrationContent, string prototypeContent)
        {
            var root = Path.Combine(Path.GetTempPath(), $"hide_spawn_menu_sync_{Guid.NewGuid():N}");
            _tempRepositories.Add(root);

            var migrationPath = Path.Combine(root, "Resources", "migration.yml");
            var prototypePath = Path.Combine(root, "Resources", "Prototypes", "Entities", "test.yml");
            Directory.CreateDirectory(Path.GetDirectoryName(migrationPath)!);
            Directory.CreateDirectory(Path.GetDirectoryName(prototypePath)!);

            File.WriteAllText(migrationPath, NormalizeMultiline(migrationContent));
            File.WriteAllText(prototypePath, NormalizeMultiline(prototypeContent));
            return root;
        }

        private static string ReadPrototype(string root)
        {
            var prototypePath = Path.Combine(root, "Resources", "Prototypes", "Entities", "test.yml");
            return File.ReadAllText(prototypePath);
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
            return input.Split(value, StringSplitOptions.None).Length - 1;
        }
    }
}
