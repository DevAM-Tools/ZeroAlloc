<!-- Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information. -->

# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - 2026-06-17

### Added

- `ZeroAlloc.Generator.Tests` project with Roslyn harness and exit-point coverage for the source generator.
- Local exit-point coverage gate via `coveragegap.tool` 2.0.0 (`dotnet tool run coveragegap run solution ...`).
- `nuget.config.example` for optional local package feeds during development.

### Changed

- Build configuration simplified: pack logic in `build/ZeroAlloc.Pack.targets`, generator props shipped as `buildTransitive/ZeroAlloc.props`, NuGet metadata scoped to packable projects.
- Target **.NET 10** with **C# 14** (`LangVersion` 14, `ImplicitUsings` enabled solution-wide).
- Expanded test suite (1949 tests) with full exit-point coverage on `ZeroAlloc` and `ZeroAlloc.Generator`.
- `LazyString` atomic evaluation and deferred formatting refinements.
- Builder grow-stall safety guards and buffer growth paths in `TempStringBuilder` / `TempBytesBuilder`.
- Generator: `_DescendantUsesZeroAllocApi` replaces `_ContainsZeroAllocApiCalls` (fewer false positives).
- `CSharpStyleValidator` 2.0.0 via central package management.
- Release builds scope `ContinuousIntegrationBuild` / `PathMap` to explicit pack only (fixes Release coverage PDB paths).
- NuGet package no longer requires license acceptance prompt (`PackageRequireLicenseAcceptance=false`).

### Fixed

- Empty Cobertura output in Release when `PathMap` was applied globally.
- Generator `_GetUserClassInfo` handling for partial classes without a base list.
- `DispatchProxy`-based generator reflection tests on sealed types.

### Removed

- Duplicate `GenDebug` project under `ZeroAlloc.Generator.Tests` (generator debugging covered by `ZeroAlloc.Generator.Tests`).

## [0.4.0]

Prior releases: see git tags `0.1.0` through `0.4.0`.

## Release process (manual)

Prerequisites: .NET SDK 10 (`global.json`), `dotnet tool restore` (installs `coveragegap.tool` from `.config/dotnet-tools.json`).

```bash
# 1. Quality gate
dotnet build ZeroAlloc.slnx -c Release
dotnet test ZeroAlloc.slnx -c Release
dotnet tool run coveragegap run solution ZeroAlloc.slnx --repo-root . --configuration Release --format agent
# Expect: exitGapCount == 0, gatePassed == true

# 2. Pack (deterministic sources for symbols)
dotnet pack ZeroAlloc/ZeroAlloc.csproj -c Release -p:ContinuousIntegrationBuild=true -o artifacts

# 3. Publish to nuget.org (requires API key)
dotnet nuget push artifacts/ZeroAlloc.0.5.0.nupkg --source https://api.nuget.org/v3/index.json --api-key <NUGET_API_KEY>
dotnet nuget push artifacts/ZeroAlloc.0.5.0.snupkg --source https://api.nuget.org/v3/index.json --api-key <NUGET_API_KEY>
```

After publish: create git tag `v0.5.0` and GitHub release with this changelog section.
