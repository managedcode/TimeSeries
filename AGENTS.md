# AGENTS.md

Project: ManagedCode.TimeSeries
Stack: .NET 10 (net10.0) / C# 14 with Nullable + ImplicitUsings; xUnit + Shouldly tests; Orleans serialization adapters; BenchmarkDotNet; coverlet; central package versions via Directory.Packages.props.

Follows [MCAF](https://mcaf.managed-code.com/)

---

## Conversations (Self-Learning)

Learn the user's habits, preferences, and working style. Extract rules from conversations, save to "## Rules to follow", and generate code according to the user's personal rules.

**Update requirement (core mechanism):**

Before doing ANY task, evaluate the latest user message.
If you detect a new rule, correction, preference, or change → update `AGENTS.md` first.
Only after updating the file you may produce the task output.
If no new rule is detected → do not update the file.

**When to extract rules:**

- prohibition words (never, don't, stop, avoid) or similar → add NEVER rule
- requirement words (always, must, make sure, should) or similar → add ALWAYS rule
- memory words (remember, keep in mind, note that) or similar → add rule
- process words (the process is, the workflow is, we do it like) or similar → add to workflow
- future words (from now on, going forward) or similar → add permanent rule

**Preferences → add to Preferences section:**

- positive (I like, I prefer, this is better) or similar → Likes
- negative (I don't like, I hate, this is bad) or similar → Dislikes
- comparison (prefer X over Y, use X instead of Y) or similar → preference rule

**Corrections → update or add rule:**

- error indication (this is wrong, incorrect, broken) or similar → fix and add rule
- repetition frustration (don't do this again, you ignored, you missed) or similar → emphatic rule
- manual fixes by user → extract what changed and why

**Strong signal (add IMMEDIATELY):**

- swearing, frustration, anger, sarcasm → critical rule
- ALL CAPS, excessive punctuation (!!!, ???) → high priority
- same mistake twice → permanent emphatic rule
- user undoes your changes → understand why, prevent

**Ignore (do NOT add):**

- temporary scope (only for now, just this time, for this task) or similar
- one-off exceptions
- context-specific instructions for current task only

**Rule format:**

- One instruction per bullet
- Tie to category (Testing, Code, Docs, etc.)
- Capture WHY, not just what
- Remove obsolete rules when superseded

---

## Rules to follow (Mandatory, no exceptions)

### Commands

- build: `dotnet build ManagedCode.TimeSeries.slnx --configuration Release`
- test: `dotnet test ManagedCode.TimeSeries.Tests/ManagedCode.TimeSeries.Tests.csproj --configuration Release`
- format: `dotnet format ManagedCode.TimeSeries.slnx`
- coverage: `dotnet test ManagedCode.TimeSeries.Tests/ManagedCode.TimeSeries.Tests.csproj --configuration Release -p:CollectCoverage=true -p:CoverletOutputFormat=lcov`

### Task Delivery (ALL TASKS)

- Architecture-first, no exceptions: always start from `docs/Architecture/Overview.md` to locate modules and boundaries; if it is missing, stop and ask for bootstrap (do not add file-creation logic to AGENTS.md).
- Git history pattern: commit messages are short, lowercase summaries without conventional prefixes; keep under ~60 chars.
- Branch naming: only `main` is present; if a branch is needed, use short lowercase names (no new scheme).
- For code modification tasks: run `dotnet build ManagedCode.TimeSeries.slnx --configuration Release` before changes to capture error count, then run it again after; only commit if the error count stays the same or decreases, otherwise revert.
- Any AGENTS.md updates from conversation context must be added under "## Rules to follow."
- Domain context (feature/bug work): this library aggregates events into time-series metrics via accumulators and summers; prioritize low allocations and lock-free paths.
- After every build you run for a code-change task, run `dotnet format ManagedCode.TimeSeries.slnx`; if formatting changes files, rebuild to confirm the error count still does not increase.
- The architecture overview must include detailed workflow diagrams and explanations for accumulators and summers so their operation is clear.
- All public types must remain thread-safe for concurrent reads/writes; add multi-threaded tests to validate this.
- Fix all build/test warnings before finalizing work.
- Performance is critical: prefer non-blocking, allocation-conscious patterns; avoid `ToArray()`/sorting in hot paths unless justified.
- README and docs must use professional tone (no informal phrases like “YouTube-style”).
- Document time handling as “UTC-normalized”: APIs accept any offset, but values are normalized to UTC internally; avoid phrasing “UTC only.”
- Ensure public API semantics are reviewed and documented; add XML documentation to public methods/properties.
- Orleans 9 serialization guidance in docs must match official configuration patterns; do not document unsupported `AddSerializer(...AddConverter...)` usage.
- Orleans converters must exist for all introduced public time-series collection types (accumulators/summers/groups).
- When requested, perform a final review pass and report readiness for release.
- Consider single-threaded Orleans scenarios when choosing concurrent collections; document the rationale for keeping or changing `ConcurrentQueue` and do not weaken thread-safety guarantees.

### Testing

- Tests live in `ManagedCode.TimeSeries.Tests` and use xUnit + Shouldly; helper assertions are in `ManagedCode.TimeSeries.Tests/Assertions/ShouldlyExtensions.cs`.
- Test files are organized by feature (`AccumulatorsTests`, `SummersTests`, `TimeSeriesBehaviorTests`, `TimeSeriesAdvancedTests`); most tests use `[Fact]`, async tests are acceptable.
- Avoid skipped tests; if a skip is unavoidable, document the unblocker in-code.
- Ensure all tests pass after changes; add or expand tests to increase coverage when behavior changes or bugs are fixed.
- When a review/report lists issues, implement fixes for all listed findings unless explicitly scoped out.
- Measure code coverage; if it drops below 90%, add tests to restore it (including multi-threaded coverage where appropriate).

### Code Style

- File-scoped namespaces, four-space indentation, braces on new lines.
- Naming: PascalCase for types/members, `_camelCase` for private fields, `camelCase` for locals/parameters, PascalCase constants.
- Nullable reference types + implicit usings are enabled (see `Directory.Build.props`); use null-forgiving (`!`) sparingly.
- `var` is used when the type is obvious; explicit types are used when clarity matters.
- Target net10.0 / C# 14; prefer new language/runtime features when they improve clarity or reduce allocations.
- Prefer unsigned numeric types for values that can never be negative; avoid negative sentinel values (use `0` for “unbounded”).
- Prefer `DateTime.UtcNow` for timestamps unless offset is required; avoid storing offsets in hot paths.
- Prefer `System.Text.Json` for serialization/deserialization paths.
- Rename public surface areas when a clearer domain term improves correctness (e.g., “Samples” as time-series samples, not examples).

### Boundaries

- Core lock-free time-series logic in `ManagedCode.TimeSeries/Abstractions`, `ManagedCode.TimeSeries/Accumulators`, and `ManagedCode.TimeSeries/Summers` is performance-critical; avoid adding locks or extra allocations.
- Orleans serialization adapters in `ManagedCode.TimeSeries.Orleans` are compatibility boundaries; keep converter/surrogate changes backward-compatible.
- Central build + version config lives in `Directory.Build.props` and `Directory.Packages.props`; change cautiously.
- Benchmarks in `ManagedCode.TimeSeries.Benchmark` are the performance baseline when optimizing.
- Favor non-blocking collections and patterns over locking when optimizing concurrency.

---

## Preferences

### Likes

### Dislikes
