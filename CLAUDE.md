# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

`LoDaTek.Cloneable` — a C# source generator that writes a `Clone()` method for classes marked `[Cloneable]` (fork of Nickztar/Cloneable, itself a fork of mostmand/Cloneable). Two NuGet packages, a test project and a sample:

- `Cloneable/` (`LoDaTek.Cloneable`) — the Roslyn generator (`CloneableGenerator`, `CloneCodeBuilder`, `SymbolExtensions`). Packed as an analyzer (`analyzers/dotnet/cs`); it has no lib assemblies by design (`NU5128` is suppressed).
- `LoDaTek.Cloneable.Core/` (`LoDaTek.Cloneable.Core`) — the attributes (`Cloneable`, `Clone`, `IgnoreClone`) that consumers reference.
- `Clonable.Test/` — xunit tests (note the spelling of the folder), `net10.0;net11.0`.
- `Cloneable.Sample/` — console sample, `net10.0;net11.0`.

## Build and test

```bash
dotnet restore Cloneable.sln
dotnet build Cloneable.sln -c Release
dotnet test Clonable.Test/Clonable.Test.csproj -c Release
```

- The generator and the Core library stay `netstandard2.0`: an analyzer must target `netstandard2.0` to load in every compiler, and the attributes work for every consumer from .NET Framework to .NET 11. The tests and sample build with the generator on .NET 10 and .NET 11 to prove it works with those compilers. Build with the .NET 10 and .NET 11 SDKs (`global.json` rolls forward to the latest major).
- The generator is an incremental generator (`IIncrementalGenerator`, `ForAttributeWithMetadataName`) built on `Microsoft.CodeAnalysis.CSharp` 4.3.1, so consumers need Visual Studio 2022 17.3 or the .NET 6.0.4xx SDK or later. The pipeline passes plain strings (generated source) rather than symbols, so results cache between edits. Analyzer rules are enforced (`EnforceExtendedAnalyzerRules`): do not use `Environment`, files or the network in the generator. `CloneCodeBuilder` holds the code generation; `CloneableGenerator` is only the pipeline.
- `GeneratePackageOnBuild=true` on both packages, so every build produces `.nupkg` files (with `README.md` and `DSIcon.png`). Release builds add SourceLink.
- Null collection properties (lists, arrays, dictionaries, enumerables, including nested ones) are cloned as `null`: the generated code is wrapped in `(x == null ? null : ...)`. `NullCollectionClone` in `Clonable.Test` covers it. A null property whose type is itself `[Cloneable]` and not annotated nullable still throws `NullReferenceException` on purpose (`ThrowsOnNonNullable`).

## Branching

- `development` is the working branch: branch from it and open pull requests into it. Never commit or open a pull request directly against `main`.
- `main` is the release branch. Every push to `main` runs `release.yml`, which publishes the packages to nuget.org and creates a GitHub release, so only merge `development` into `main` when a release is intended, and only when asked to release.

## CI

GitHub Actions (`.github/workflows/`) on `windows-latest`:

- `ci.yml` builds Release and runs the tests for pull requests into `main` and `development`; it publishes nothing.
- `release.yml` runs on every push to `main` (changes only to Markdown or workflow files are skipped; run it by hand to test a workflow change). It builds Release as `3.0.<yyMM>.<run number>` plus `RELEASE_SUFFIX` (empty for a stable version), runs the tests, pushes `LoDaTek.Cloneable` and `LoDaTek.Cloneable.Core` to nuget.org with Trusted Publishing (OIDC, `NUGET_USER` secret, `nuget` environment), then tags the commit `v<version>` and creates a GitHub release with the packages attached.
