# C# Standards

Load when `*.cs` files are in scope. Extends Section 4 in `copilot-instructions.md`.

## Language
- Target .NET 10 (`net10.0`), C# 14. Modern idiomatic C#.
- Apply build settings from `tech-solution.md` when build files in scope.

## Naming
- ❗Never `var`. Use `new()` / `[]` instead of repeating the type;
- ❗`_PascalCase` for private fields, properties, methods, statics.

## Files & Usings
- File-scoped namespaces.
- Global usings in `GlobalUsings.cs` only; no per-file namespace usings. File-local type aliases (`using Alias = ...`) are allowed in source files.
- Sort: `System.*` → `Microsoft.*` → third-party → internal.

## Style
- Always brace control-flow blocks.
- At most one callable exit point per source line
- Expression-bodied members for simple single expressions.
- `get; init;` on interface read-only properties.
- Never `.Result` or `.Wait()` — `async`/`await` only.
- Avoid exceptions for expected failures. Prefer `Try*` APIs, result types, or error codes — especially in hot paths.
- Evaluate `ValueTask` for often-synchronous `Task` APIs.
- `using` declarations for method-scoped disposables.
- `sealed` on non-inheritable classes.
- `readonly` fields/properties where possible.
- `#region` only to structure large files.

## Integer arithmetic
- ❗Assess every integer op for overflow/underflow; use `checked`, widen, or validate when wrap-around would be wrong.
- Document proven-safe ranges; use `unchecked` in hot paths only then.
- Prove bounds at boundaries; no redundant overflow checks in inner loops.

## Formatting
- 4-space indent; no tabs.
- Follow `IDisposable` / `IAsyncDisposable` patterns.
- Decompose complex methods into focused helpers.
- UTF-8 console encoding in CLI startup.
- Name threads; `CultureInfo.InvariantCulture` for thread culture.
- No console/trace for library error handling.
- Document physical units in comments, not variable names.
