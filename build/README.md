<!-- Copyright © 2026 DevAM. All rights reserved. Licensed under MIT license. See license in the repository root for license information. -->

# Build layout

## Shipped in the `ZeroAlloc` NuGet package

| Path in package | Purpose |
|-----------------|---------|
| `lib/net10.0/ZeroAlloc.dll` | Runtime library |
| `lib/net10.0/ZeroAlloc.xml` | API documentation |
| `analyzers/dotnet/cs/ZeroAlloc.Generator.dll` | Roslyn source generator |
| `buildTransitive/ZeroAlloc.props` | Optional MSBuild tuning (`ZeroAlloc_*` properties) |
| `README.md`, `icon.png` | Package metadata |

Consumers: `dotnet add package ZeroAlloc` and define `internal partial class ZA : ZeroAllocBase { }`.

## Repository only (not in NuGet)

| File | Purpose |
|------|---------|
| `Directory.Build.props` | Shared compile settings and release metadata |
| `Directory.Build.targets` | CSharpStyleValidator (quality gate; `PrivateAssets=all` so it is not packed as a consumer dependency) |
| `Directory.Packages.props` | Central package versions |
| `build/ZeroAlloc.Pack.targets` | Pack output, analyzer inclusion, buildTransitive props |
| `ZeroAlloc.Generator/build/ZeroAlloc.Generator.props` | Source for `buildTransitive/ZeroAlloc.props` (imported in-repo via `ZeroAlloc.csproj`) |
