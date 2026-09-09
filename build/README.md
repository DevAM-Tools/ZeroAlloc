<!-- Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information. -->

# Build layout

## Shipped in the `ZeroAlloc` NuGet package

| Path in package | Purpose |
|-----------------|---------|
| `lib/net10.0/ZeroAlloc.dll` | Runtime library |
| `lib/net10.0/ZeroAlloc.xml` | API documentation |
| `analyzers/dotnet/cs/ZeroAlloc.Generator.dll` | Roslyn source generator |
| `buildTransitive/ZeroAlloc.props` | Optional MSBuild tuning (`ZeroAlloc_*` properties) |
| `buildTransitive/ZeroAlloc.targets` | Applies `ZeroAlloc_GenerateDefaultClass` opt-out (`ZEROALLOC_SUPPRESS_DEFAULT_ZA`) |
| `README.md`, `icon.png` | Package metadata |

Consumers: `dotnet add package ZeroAlloc` then `using ZeroAlloc;` and `ZA.String(...)`. No handwritten `ZeroAllocBase` subclass is required. Extra subclasses remain optional. Set `ZeroAlloc_GenerateDefaultClass` to `false` to suppress default `ZA`.

## Repository only (not in NuGet)

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Shared compile settings and release metadata |
| `Directory.Build.targets` | CSharpStyleChecker (quality gate; not packed as a consumer dependency) |
| `Directory.Packages.props` | Central package versions |
| `build/ZeroAlloc.Pack.targets` | Pack output, analyzer inclusion, buildTransitive props and targets |
| `ZeroAlloc.Generator/build/ZeroAlloc.Generator.props` | Source for `buildTransitive/ZeroAlloc.props` (imported in-repo via `ZeroAlloc.csproj`) |
| `ZeroAlloc.Generator/build/ZeroAlloc.Generator.targets` | Source for `buildTransitive/ZeroAlloc.targets` (imported in-repo via `Directory.Build.targets`) |
