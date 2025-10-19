# Conversations
any resulting updates to agents.md should go under the section "## Rules to follow"
When you see a convincing argument from me on how to solve or do something. add a summary for this in agents.md. so you learn what I want over time.
If I say any of the following point, you do this: add the context to agents.md, and associate this with a specific type of task.
if I say "never do x" in some way.
if I say "always do x" in some way.
if I say "the process is x" in some way.
If I tell you to remember something, you do the same, update


## Rules to follow (SUPER IMPORTANT)
For any code modification task: run dotnet build before applying changes to capture the current error count. After the changes, run dotnet build again and only commit if the error count stays the same or decreases; revert if it increases.


# Repository Guidelines

## Project Structure & Module Organization
- `ManagedCode.TimeSeries/` — core library with accumulators, summers, and shared abstractions.
- `ManagedCode.TimeSeries.Tests/` — xUnit test suite referencing the core project; coverage instrumentation configured via coverlet.
- `ManagedCode.TimeSeries.Orleans/` — Orleans-specific adapters built atop the core types.
- `ManagedCode.TimeSeries.Benchmark/` — BenchmarkDotNet harness for performance tracking, entry point in `Program.cs`.
- `Directory.Build.props` centralizes NuGet metadata, reproducible build settings, and solution-wide assets.

## Build, Test, and Development Commands
- `dotnet restore` — fetch solution dependencies.
- `dotnet build --configuration Release` — compile all projects and validate analyzers.
- `dotnet test ManagedCode.TimeSeries.Tests/ManagedCode.TimeSeries.Tests.csproj -p:CollectCoverage=true` — run tests with coverlet lcov output (mirrors CI).
- `dotnet run --project ManagedCode.TimeSeries.Benchmark/ManagedCode.TimeSeries.Benchmark.csproj -c Release` — execute benchmarks before publishing performance-sensitive changes.
- `dotnet pack ManagedCode.TimeSeries/ManagedCode.TimeSeries.csproj -c Release` — produce the NuGet package using metadata from `Directory.Build.props`.

## Coding Style & Naming Conventions
Target `net9.0` with C# 13, `Nullable` and `ImplicitUsings` enabled; favour modern language features when they improve clarity. Stick to four-space indentation, braces on new lines, PascalCase for types, methods, and public properties, camelCase for locals and parameters, and ALL_CAPS only for constants. Keep namespaces aligned with folder paths under `ManagedCode.TimeSeries`.

## Testing Guidelines
Use xUnit `[Fact]` and `[Theory]` patterns with Shouldly for assertions; prefer descriptive test method names (`MethodUnderTest_State_Expectation`). Ensure new logic includes coverage and leaves no skipped tests behind; if a skip is unavoidable, document the unblocker in-code. Generate fresh coverage via the command above before opening a PR and verify lcov output lands under `ManagedCode.TimeSeries.Tests/`.

## Commit & Pull Request Guidelines
Follow the repo’s concise commit style (`scope summary` or short imperative lines under ~60 characters, e.g., `bench improve queue path`). Each PR should describe the change, reference related issues, and call out API or serialization impacts. Include evidence of local `dotnet test` runs (and benchmarks when relevant) plus screenshots for user-facing changes. Highlight breaking changes or version bumps so release automation remains accurate.
