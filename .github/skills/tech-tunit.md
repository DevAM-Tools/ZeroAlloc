# TUnit Testing Rules

Load when test files or test projects are in scope. Implements Section 4.5 enforcement in `copilot-instructions.md`.

## Scope

- Unit tests in `.Tests` projects; bUnit component tests for Razor/Blazor → `tech-blazor.md`. Exit-point gate on testable `.razor.cs` and service logic.

## Framework

- Use TUnit
- Make every test method `async Task`.
- Use NSubstitute for doubles.
- Enable Microsoft Testing Platform (MTP) in repo-root `global.json` — required for `dotnet test` with TUnit on .NET SDK 10+:

```json
{
  "test": {
    "runner": "Microsoft.Testing.Platform"
  }
}
```

- No extra coverage NuGet package: MTP + TUnit on .NET SDK 10+; `dotnet tool run coveragegap run` passes `--coverage --coverage-output-format cobertura` to `dotnet test`. Do not add `coverlet.collector` (VSTest-only; incompatible with MTP).

## Test Quality

- Never use `Thread.Sleep` in tests.
- Cover happy path, errors, edges, boundaries, corners.
- Cover `null`, empty, min/max, off-by-one, max-length strings, collections 0/1/2, concurrency, branch-straddling values.
- Use data-driven tests (`[Arguments(...)]`, `[MethodDataSource(...)]`).
- Keep tests deterministic, independent, order-insensitive.
- Run tests on Windows/Linux/macOS and x64/ARM64.
- Assert one logical outcome per test.

## CoverageGap.Tool

❗ Mandatory NuGet local tool at version **`2.*`** on every repo with test projects. SSOT for exit-point coverage.

**Gate:** `summary.exitGapCount == 0`. Branch fields informational. **`run`** gates class-library projects only (`OutputType` `Exe` excluded).

**Setup** (once per repo root):

```bash
dotnet new tool-manifest
dotnet tool install CoverageGap.Tool --version 2.*
```

Fresh clone / new machine: `dotnet tool restore` before first run.

**Gate** (primary):

```bash
dotnet tool run coveragegap --repo-root .
```

Auto-discovers `.slnx`/`.sln`; pairs `{Project}.Tests`; runs MTP Cobertura + exit-gap report. Exit codes: `0` pass · `1` gap/failure · `2` usage.

| Task | Command |
|------|---------|
| Gate solution | `dotnet tool run coveragegap run solution path/File.slnx --repo-root . --configuration Release --format agent` |
| Gate project(s) | `dotnet tool run coveragegap run project path/Proj.csproj --repo-root .` |
| Plan exits (no tests) | `dotnet tool run coveragegap plan project path/Proj.csproj -o exits.json --repo-root .` |

Fix every `exitGaps[]` entry (`file`, `line`, `exitPointId`, `kind`); re-run until `exitGapCount == 0`. No tests yet: `plan` → add tests → gate.

Pairing override: `--test-project`. Parallel-safe: isolated `--work-dir`; relative `-o` under work dir. Flags: `--skip-no-tests`, `--no-build`, `--cobertura <file>`, `--include-snippet`, `--keep-work-dir`.

**This repository (contributors):** `dotnet run --project src/CoverageGap.Tool -c Release -- run --repo-root .`

## Structure

- Name test project `<ProductionProjectName>.Tests`.
- Mirror production namespace and folder structure.
- Use one test file per production class: `<ClassName>Tests.cs`.
- Put shared helpers in `Helpers/`.
- Name test methods `<Method>_<Scenario>_<ExpectedResult>`.
- Name method data sources `<Method>_<Scenario>_Data`.

## Authoring

- Separate Arrange, Act, Assert with blank lines.
- Use `await Assert.That(actual).Is...`.
- Use builders for non-trivial setup.
- Use `[Arguments(...)]` for corner cases.
- Use `[MethodDataSource(...)]` for reusable data sets.
- Always await async operations.
- Assert exceptions: `await Assert.That(async () => await sut.Method()).Throws<ExceptionType>()`.
- Pass `CancellationToken` to cancellation-aware APIs; test cancellation.

## Fixtures and Parallelism

- Use `[Before(Test)]` / `[After(Test)]` for per-test setup.
- Use `[Before(Class)]` / `[After(Class)]` for class resources.
- Implement `IAsyncDisposable` on test classes holding resources.
- Keep tests parallel-safe by default.
- Avoid shared mutable statics.
- Use `[NotInParallel]` only when required; document reason in XML.
- Run coordinated concurrent tasks in concurrency tests; verify no corruption, deadlock, or data loss.

## Doubles and Coverage

- Prefer real implementations when deterministic.
- Substitute only external or non-deterministic dependencies.
- Prefer outcome assertions over interaction counts.
- Never relax access modifiers for tests only.
- Document `[ExcludeFromCodeCoverage]` with XML reason; excluded exits are omitted from the exit-coverage gate.
