# AGENTS.md

## Policy

The stack documented below is the default and takes priority over
whatever an agent might otherwise reach for. Prefer what's already in use
over introducing an alternative. If a deviation seems necessary, say so
explicitly to the user and get confirmation before adding it.

If a change affects the folder structure or the tech stack (a new/removed project, a new dependency, a version bump worth recording, a new convention), update this file accordingly as part of the same change - don't leave it to a later pass.

Keep `CHANGELOG.md` up to date, following [Keep a Changelog](https://keepachangelog.com/) format. Add an entry under an `## [Unreleased]` heading (grouped into `Added`/`Changed`/`Fixed`/`Removed` as needed) as part of the same change that introduces it - don't leave it to a later pass, and don't wait to be asked. At release time, rename `[Unreleased]` to the version being tagged (matching the same version used in `AnalyzerReleases.Shipped.md` and the git tag) and start a fresh empty `[Unreleased]` heading above it.

Before changing existing tests or writing new ones, ask the user first – confirm what should be covered and how (or that the change is trivial enough not to need it) rather than deciding unilaterally.

Commit messages follow Conventional Commits (`<type>(<scope>): <description>`, e.g. `feat(manifest): ...`, `fix(...): ...`, `docs(agents): ...`, `test(...): ...`, `chore(...): ...`, `refactor(...): ...`) - scope optional but preferred when it clarifies what changed.

Don't `git push` - commit locally and leave pushing to the user, unless they explicitly ask for a push.

## About

Source for `XamlConstructor` (NuGet package `XamlConstructor`): a C#
incremental source generator that emits a parameterless "XAML Design
Mode" constructor for any class or struct decorated with
`[XamlConstructor]` and declared `partial`, assigning every private/
protected readonly field to `null!`. Intended for WPF/XAML view-models
that need a design-time-only constructor (so the XAML designer can
instantiate them) without hand-writing and maintaining one. Despite
living alongside the `toolkit.revit.*` family, it has **no dependency on
the Revit API or WPF** - it's a plain Roslyn analyzer/generator package,
usable in any C# project.

Standard Roslyn analyzer/generator layout: the generator and its
companion "type must be partial" analyzer + code fix live in separate
`netstandard2.0` projects, and are packaged together into a single
`analyzers/dotnet/cs` NuGet package by a fourth, code-free packaging
project.

## Repository structure

```
.
├── src/
│   ├── XamlConstructor/                 packaging project only (no source files) -
│   │                                    packs XamlConstructor.Generator.dll and
│   │                                    XamlConstructor.CodeFixes.dll as analyzers
│   │                                    and publishes the NuGet package
│   ├── XamlConstructor.Generator/       the incremental generator (XCONS01/XCONS02
│   │   │                                diagnostics) + the "type without partial" analyzer
│   │   ├── Analyzers/
│   │   ├── Extensions/
│   │   └── Generators/
│   └── XamlConstructor.CodeFixes/       code fix provider for the XCONS01 diagnostic
│                                        (adds the missing `partial` modifier)
└── tests/
    └── XamlConstructor.Tests/           references the Roslyn analyzer/code-fix/source-
                                         generator testing packages, but currently has
                                         no test files - see note below
```

## Tech stack

- Roslyn incremental generator (`IIncrementalGenerator`) + `DiagnosticAnalyzer` +
  `CodeFixProvider`, all targeting `netstandard2.0` (required for analyzers/generators)
- Central package management (`Directory.Packages.props`,
  `ManagePackageVersionsCentrally=true`) **is** used here - unlike most of the
  `toolkit.*`/`toolkit.revit.*` family, which leaves `Directory.Packages.props`
  empty and pins versions per-`<PackageReference>`
- Repo-wide global analyzers pinned in `Directory.Packages.props`:
  Roslynator.Analyzers, SonarAnalyzer.CSharp
- MinVer (git-tag-based semantic versioning, tag prefix `v`), matching the
  rest of the family
- Tests target `net10.0` and use xunit directly against the raw Roslyn APIs
  (`CSharpCompilation`, `CompilationWithAnalyzers`, `CSharpGeneratorDriver`,
  `AdhocWorkspace`) rather than the `Microsoft.CodeAnalysis.CSharp.{Analyzer,
  CodeFix,SourceGenerators}.Testing` helper packages - those were referenced
  early on but never actually used in test code, so they were dropped
- `Microsoft.CodeAnalysis` / `.CSharp` / `.CSharp.Workspaces` are exact-pinned
  (`[4.14.0]`, not a floor) in `Directory.Packages.props` - this is the
  compiler version the analyzer/generator/code-fix assemblies are built
  against, and it also governs which C# syntax test sources may use when
  parsed directly via `CSharpSyntaxTree`/`CSharpCompilation` at test time.
  `4.14.0` matches the Roslyn version shipped with the latest Visual Studio
  2022 release line, so the analyzer stays loadable by any current VS 2022
  install. Bumping past the VS-2022-compatible range (e.g. into the `5.x`
  series, which targets newer hosts) is a deliberate compatibility trade-off,
  not a routine "latest version" update - if it's ever done, call it out in
  the README/release notes so consumers on older hosts aren't surprised

## Notes

- `src/Xaml.Generator/` (a stray, unreferenced `net10.0` scaffold project with
  no source files, left over from an early restructuring) was removed - it
  wasn't part of the `.slnx` and duplicated no functionality that
  `XamlConstructor.Generator` doesn't already cover.
