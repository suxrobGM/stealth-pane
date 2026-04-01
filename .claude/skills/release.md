---
name: release
description: Bump version, update changelog, commit, and create a git tag for a new release
user-invocable: true
---

# Release Skill

Create a new release by bumping the version, updating the changelog, committing, and tagging.

## Usage

The user provides a version number (e.g., `1.0.3`) or a bump type (`major`, `minor`, `patch`).

If no argument is given, default to a `patch` bump based on the current version.

## Steps

1. **Determine the new version:**
   - Read the current version from `src/StealthCode/StealthCode.csproj` (the `<Version>` element)
   - If the user provided a specific version (e.g., `1.0.3`), use that
   - If the user provided a bump type (`major`, `minor`, `patch`), calculate the new version from the current one
   - If no argument was given, bump the patch version

2. **Bump version in all three project files:**
   - `src/StealthCode/StealthCode.csproj`
   - `src/StealthCode.Launcher/StealthCode.Launcher.csproj`
   - `src/StealthCode.Updater/StealthCode.Updater.csproj`
   - Replace the `<Version>X.Y.Z</Version>` value in each file

3. **Update CHANGELOG.md:**
   - Read the existing `CHANGELOG.md`
   - Ask the user what changes to include in this release (or let them confirm if they've already written the entries)
   - Add a new `## [X.Y.Z] - YYYY-MM-DD` section at the top (below the header), using today's date
   - Do NOT remove or modify existing entries

4. **Commit the changes:**
   - Stage the three `.csproj` files and `CHANGELOG.md`
   - Commit with message: `release: vX.Y.Z`

5. **Create a git tag:**
   - Create an annotated tag: `git tag vX.Y.Z`

6. **Report the result:**
   - Show the new version, changed files, and the tag name
   - Remind the user to `git push && git push --tags` to trigger the release workflow
