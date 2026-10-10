<!-- Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information. -->

# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.6.1] - 2026-10-10

ZeroAlloc 0.6.1 hardens buffer ownership and LazyString failure reporting, and ships the agent workflow surface (`/requirements`, `/illustrate`, `/commit-message`) used to develop the library.

### Added

- `LazyString.TryGetString(out string, out LazyStringError)` with `LazyStringError` / `LazyStringErrorCode` for factory faults.
- `ZeroAllocHelper.AcquireCharBuffer` / `AcquireByteBuffer` overloads that return the acquire `epoch` alongside `isThreadStatic`.
- Agent workflows: `/requirements` (`REQ-<AREA>-<TOPIC>`), `/illustrate` (HTML zip demo), `/commit-message` (paste-ready English message for a named scope). Cursor commands and skill pointers under `.cursor/` match `.github/` SSOT.

### Changed

- TempStringBuilder / TempBytesBuilder capacity and position math use checked addition (overflow throws `OverflowException`).
- CSharpStyleChecker central version pinned to `1.0.1`.
- README Quick Start leads with `dotnet add package ZeroAlloc`; Installation is TOC item 2.
- BitReader / BitWriter XML summaries document single-thread / not-thread-safe use.
- Packable `VersionPrefix` is `0.6.1`.
- Plan, review, implement, council, and complex-task skills tightened (requirements-first planning, release verdict on `/review`, Closing Exam after `/implement`).

### Breaking

- Removed parameterless `ZeroAllocHelper.ReleaseCharBuffer()` and `ReleaseByteBuffer()`. Pass the `epoch` from the matching Acquire overload (same model as `TempString` / `TempBytes` Dispose).

## [0.6.0] - 2026-09-09

ZeroAlloc 0.6.0 is a consumer-facing release: each consuming compilation gets `ZeroAlloc.ZA` without a handwritten subclass, generated overflow modes actually run, and binary string parse grows an explicit size cap.

### Added

- Per-assembly `ZeroAlloc.ZA`: after `using ZeroAlloc;`, call `ZA.String` / `ZA.Utf8` / `ZA.Bytes` / `ZA.Lazy` without declaring a `ZeroAllocBase` subclass. Extra subclasses (`MyApi : ZeroAllocBase`) still generate.
- MSBuild property `ZeroAlloc_GenerateDefaultClass` (default `true`). Set `false` to suppress default `ZA` (`ZEROALLOC_SUPPRESS_DEFAULT_ZA`). Packed as `buildTransitive/ZeroAlloc.targets`. The `ZeroAlloc` library assembly itself never contains type `ZeroAlloc.ZA`.
- `[StringMaxLength(n)]` on `[BinaryParsable]` string members. Generated `TryParse` rejects UTF-8 payloads above the cap without allocating. Default cap is 1 MiB when the attribute is omitted (diagnostic **ZA2016**, Info).
- `ZeroAlloc_BufferOverflowBehavior` is honored for **generated** `ZA.String` / `ZA.Utf8` / `ZA.Bytes`: `Grow` (default, ThreadStatic), `HeapFallback` (one-shot heap copy), `Throw` (`InvalidOperationException`, buffer released). `TempStringBuilder` / `TempBytesBuilder` always grow; they do not read this property.

### Changed

- ThreadStatic `TempString` / `TempBytes` release is epoch-based: implicit conversion plus a later `Dispose` cannot unlock a newer owner's buffer.
- Buffer acquire allocates before setting in-use, so OOM cannot leave the thread lock stuck.
- `GrowCharBuffer` / `GrowByteBuffer` copy only the used prefix, not the unused tail.
- Length-prefix and bit-cursor integer hardening: `Utf8Var` / `Utf8FixBE` / `Utf8FixLE` reject oversized prefixes; `VarInt` fails leftover 10th-byte payload bits; `BitReader` / `BitWriter` use 64-bit bit capacity (`ByteLengthToBitCapacity`) instead of `Length << 3`; `BinaryParser` remaining-length checks use unsigned remaining.
- ZA1002 message names `SpanStringBuilder` (not a non-existent `ZeroStringBuilder`).
- Packable version uses `VersionPrefix` `0.6.0`. `ContinuousIntegrationBuild` is pack/CI-pack only so Release coverage PDBs stay valid.
- Local tool `exitpointgaps` 1.0.1. Central package transitive pinning enabled.

### Fixed

- Nested `using` / double `Dispose` on `TempString` / `TempBytes` unlocking the ThreadStatic buffer.
- Documented overflow switch that previously always heap-copied.
- Grow-stall test hook is `volatile`.
- Generator XML/header drift (`[BinaryRemaining]` and similar names that were never shipped).

### Breaking

- Setting `ZeroAlloc_BufferOverflowBehavior` to `Throw` now throws from generated `ZA.*` format methods when the ThreadStatic buffer is too small (previously the property was ignored).

A leftover 0.5.x `internal partial class ZA : ZeroAllocBase { }` in the consumer namespace is a **second** type, not a merge with generated `ZeroAlloc.ZA`. Delete it. A leftover `partial` in `namespace ZeroAlloc` merges with the stub. Opt out with `ZeroAlloc_GenerateDefaultClass`=`false`.

### Not in this release

- `ZA.ParseBytes` / `TryParseBytes` are not generated. Use `[BinaryParsable]` `TryParse` or `BinaryParser`.
- Passthrough interceptors / `ZA.LocalizedLazy` are not included.

## [0.5.2] - 2026-07-09

Maintenance release: quality gates moved from CSharpStyleValidator / CoverageGap.Tool to **CSharpStyleChecker** / **ExitPointGaps**. Minor library and generator fixes.

### Added

- `LazyString` comparison operators `<`, `<=`, `>`, `>=` (delegate to `CompareTo`).

### Changed

- `SpanStringBuilder` numeric/date `TryFormat` calls pass `CultureInfo.InvariantCulture`.
- Generator parameter XML docs for constructed generic types use escaped `<c>` literals (avoids CS1570/CS1584 in consumer XML-doc builds).
- Repository analysis: CSharpStyleChecker 1.x, ExitPointGaps 1.0.0, `AnalysisLevel` 10-recommended.

## [0.5.1] - 2026-07-07

Maintenance release. No consumer-facing API or runtime behavior changes.

### Changed

- Quality gates: CSharpStyleValidator 2.1.2, CoverageGap.Tool 2.1.2, floating CPM versions for several packages.

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

Prerequisites: .NET SDK 10 (`global.json`), `dotnet tool restore` (installs `exitpointgaps` from `.config/dotnet-tools.json`).

```bash
# 1. Quality gate
dotnet build ZeroAlloc.slnx -c Release
dotnet test ZeroAlloc.slnx -c Release
dotnet tool run exitpointgaps --repo-root . --configuration Release
# Expect: exitGapCount == 0, gatePassed == true

# 2. Pack (deterministic sources for symbols)
dotnet pack ZeroAlloc/ZeroAlloc.csproj -c Release -p:ContinuousIntegrationBuild=true -o artifacts

# 3. Publish to nuget.org (requires API key)
dotnet nuget push artifacts/ZeroAlloc.0.6.1.nupkg --source https://api.nuget.org/v3/index.json --api-key <NUGET_API_KEY>
dotnet nuget push artifacts/ZeroAlloc.0.6.1.snupkg --source https://api.nuget.org/v3/index.json --api-key <NUGET_API_KEY>
```

After publish: create git tag `v0.6.1` and GitHub release with this changelog section.
