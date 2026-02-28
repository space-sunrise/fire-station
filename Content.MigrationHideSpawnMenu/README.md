# Content.MigrationHideSpawnMenu

CLI tool that marks migrated entities with `HideSpawnMenu` category in their `.yml` file directly

## What it does

1. Reads `Resources/migration.yml` and collects source IDs from `oldId -> newId` where `newId` is not `null` or empty.
2. Scans `Resources/Prototypes/**/*.yml` for top-level `- type: entity` blocks.
3. For non-abstract, live prototypes with matching IDs:
   - adds `HideSpawnMenu` to inline `categories`,
   - appends it to block-list `categories`,
   - or creates `categories: [ HideSpawnMenu ]` when missing.
4. Preserves existing categories and never creates duplicate `HideSpawnMenu`.

## Commands

```bash
dotnet run --project Content.MigrationHideSpawnMenu -- sync
dotnet run --project Content.MigrationHideSpawnMenu -- check
```

Backward-compatible form is also supported:

```bash
dotnet run --project Content.MigrationHideSpawnMenu -- hide-spawn-menu sync
dotnet run --project Content.MigrationHideSpawnMenu -- hide-spawn-menu check
```

Default mode with no arguments:

```bash
dotnet run --project Content.MigrationHideSpawnMenu
```

This runs `sync`.

## Edit comment

The tool appends an edit comment to changed category lines.

1. Environment variable: `MIGRATION_HIDE_SPAWN_EDIT_COMMENT`
2. Fallback constant: `Fire edit`

## Exit codes

1. `0`: success
2. `1`: `check` found out-of-sync prototypes
3. `2`: technical failure (invalid arguments, parsing or IO failure)

## Repository root resolution

The tool resolves the repository root automatically by searching parent directories for `Resources/migration.yml`.
This allows running from nested directories like `bin/Content.MigrationHideSpawnMenu`.

## Output

Example summary:

```text
🧭 HideSpawnMenu migration sync report
-------------------------------------
⚙️  Mode: Sync
📂 Files scanned:      3836
📝 Files changed:      30
🔎 Candidates found:   56
✅ Candidates updated: 56
⏱️  Elapsed:           1,284 ms (1.28 s)
```
