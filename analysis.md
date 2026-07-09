# UBB Simulator — Deep Analysis Working Document
*Generated: 2026-07-08 | Reflects current codebase state after test infrastructure and CI setup*

---

## 0. What Changed Since Last Analysis

| Area | Before | Now |
|------|--------|-----|
| Project structure | Single `UBB.csproj` (Blazor + logic mixed) | 3-project solution: `UBB.Core` (pure C#) · `UBB` (Blazor UI) · `UBB.Tests` (xUnit) |
| Unit tests | None | 70 tests across 5 classes (100% green) |
| Coverage | Not measured | 87.9% line / 88% branch on `UBB.Core` |
| CI gates | None | `qa.yml` (build + test + coverage ≥ 80%) · `security.yml` (CVEs + SRI + Gitleaks) |
| Local agents | None | `#qa` and `#security` in `.github/agents/` |
| Copilot instructions | None | `.github/copilot-instructions.md` |
| Duplicate files | N/A | **Fixed this session** — `src/UBB/Models/` and `src/UBB/Services/` duplicates removed (were causing CS0436 warnings that would have failed `-warnaserror` CI) |
| `plan.md` | Stale architecture doc | Still stale (tracked as TD-03) |

---

## 1. Executive Summary

| Dimension | Score | Trend | Notes |
|-----------|-------|-------|-------|
| 12-Factor compliance | 7 / 12 | ↑ | CI added (Factor V), `UBB.Core` extraction improves separation |
| SOLID adherence | B+ | → | Core engine excellent; UI layer still has SRP/DIP gaps |
| Production readiness | 70% | ↑ | Testing + CI + agents added; accessibility + error boundaries still missing |
| Docs / code sync | 65% | ↓ | README still says "6 presets" and omits 5 new components; `plan.md` describes architecture that was never built |
| Tech debt | Medium | ↓ | CS0436 bug fixed; dead models still present; `UrlStateService` still unwired |

---

## 2. 12-Factor Analysis

> **Note on scope**: UBB is a pure static Blazor WASM SPA — no server process. Factors IV (backing services), VI (processes), VII (port binding), and VIII (concurrency) are trivially N/A.

### Factor I — Codebase ✅
One repo, one solution, Git-tracked. Three well-scoped projects with no circular references.
**Remaining gap:** `plan.md` at repo root describes a `BillingEngine.cs` and a 30-day simulation that was never built. This is the most misleading piece of documentation in the repo.

### Factor II — Dependencies ✅
All NuGet packages pinned to exact versions. Bootstrap 5 and JS delivered via CDN with SRI hashes. Zero known CVEs across all three projects (verified 2026-07-08).
**Remaining gap:** CDN availability is a runtime dependency with no fallback. If `jsdelivr.net` is unreachable the app loads blank. Vendoring `wwwroot/lib/` eliminates this single point of failure.

### Factor III — Config ⚠️
Billing constants, default guardrail values, and scenario preset data are all hardcoded in C#:
- `BillingConstants.cs` — credit price, seat costs, promo credit counts (promo ends Sept 2026)
- `RequestFlowState.cs` — default ULB 2500, pool 390k, etc. baked into property initialisers
- `ScenarioPresets.cs` — 15 presets with hardcoded credit figures
**Fix:** `wwwroot/billing-config.json` fetched at startup via `HttpClient`; inject as `IOptions<BillingConfig>` into engine.

### Factor IV — Backing Services N/A
No databases, queues, or external APIs. All computation is in-browser via WebAssembly.
`UrlStateService` serialises state to a URL fragment — this is the only persistence mechanism and it is **still not wired to any UI component**.

### Factor V — Build, Release, Run ✅
`dotnet publish` produces a deterministic static bundle. GitHub Actions CI (`qa.yml`) builds and tests on every push. `security.yml` adds vulnerability scanning weekly.
**Remaining gap:** No deployment step in CI — `qa.yml` builds and tests but does not publish to GitHub Pages. A `deploy.yml` targeting the `gh-pages` branch is still missing.

### Factor VI — Processes ✅ (N/A)

### Factor VII — Port Binding ✅ (N/A)

### Factor VIII — Concurrency ✅ (N/A)

### Factor IX — Disposability ⚠️
`Home.razor` correctly implements `IDisposable` and unsubscribes `State.OnChange`.
`MultiCCPanel.razor` holds `MultiCostCenterState` as component-local state — no cleanup needed (WASM GC handles it), but if the component is re-created mid-session the state resets silently with no user warning.
**Remaining gap:** No loading timeout or error fallback for WASM bundle failures. The spinner runs indefinitely on a cold-load failure.

### Factor X — Dev/Prod Parity ✅
No environment-specific code paths. `<base href="/">` is correctly set for root deployment.

### Factor XI — Logs ⚠️
Log entries are `List<string>` — unstructured, no severity levels, no timestamps on single-user flow logs (multi-CC does have timestamps via `AddLog`). There is no way to export, filter, or correlate logs across simulation runs.
**Fix:** Introduce `record SimulationLogEntry(string Level, DateTimeOffset Timestamp, string Message)`. `ExecutionLog.razor` renders from this. Enables severity filtering in UI.

### Factor XII — Admin Processes ❌
No mechanism to update billing constants (e.g. when GitHub changes promo credit amounts post-Sept 2026) without a code change and redeploy.
**Fix:** `wwwroot/billing-config.json` with a schema version field, fetched at startup.

---

## 3. SOLID Principles

### S — Single Responsibility

| Class / Component | Verdict | Issue |
|-------------------|---------|-------|
| `RequestFlowEngine` | ✅ | Pure stateless evaluation; 3 public methods each with single clear purpose |
| `BillingConstants` | ✅ | Constants and two conversion helpers only |
| `ScenarioPresets` | ✅ | Data only |
| `AppStateService` | ⚠️ | Manages state + triggers re-renders + applies presets + calls engine + tracks active preset key. Consider extracting `SimulationRunner` |
| `Home.razor` | ⚠️ | 240-line component: layout, input binding, mode switching, field parsing, preset badge rendering. Manageable now but will grow |
| `MultiCCPanel.razor` | ⚠️ | Configuration UI + preset selection + simulation execution + result rendering + node state display (~240 lines) |
| `UBB.Core` as a whole | ✅ | Pure domain logic, no Blazor/UI dependencies |

### O — Open/Closed ⚠️
Adding a new simulation mode (e.g. Monthly 30-day) requires modifying:
- `SimulationMode` enum — adding a value ✅ (extension)
- `Home.razor` — mode tab, conditional rendering blocks ❌ (modification)
- `AppStateService.Run()` switch ❌ (modification)

A `ISimulationMode` interface with `Render()` / `Run()` would make new modes additive only.

### L — Liskov Substitution ⚠️
Node-state dictionaries (`Dictionary<string, FlowNodeState>`) use magic string keys. The single-user flow uses 6 keys (`user`, `pool`, `paid`, `costCentre`, `enterprise`, `result`); the multi-CC per-CC diagram uses 5 (no `user`). Consumers call `.TryGetValue` defensively but the contract is entirely implicit and undocumented.
**Fix:** Replace string keys with a `FlowNode` enum. Compile-time safety, no magic strings.

### I — Interface Segregation ✅ (mostly)
Components that only read state (`StatCards`, `FlowDiagram`, `ExecutionLog`) receive `RequestFlowState` as a `[Parameter]` — correctly separated from the full `AppStateService`.
**Remaining gap:** `MultiCCPanel` has no parameter interface — it accesses `ScenarioPresets` and `RequestFlowEngine` as static classes directly.

### D — Dependency Inversion ❌ (partially fixed, partially not)
`MultiCCPanel.razor` calls `RequestFlowEngine.RunMultiCostCenter()` **directly** — it bypasses `AppStateService` entirely. This means:
- Multi-CC simulation state lives only in local component state (not in `AppStateService`)
- The multi-CC `MultiCostCenterState` is not accessible from anywhere outside `MultiCCPanel`
- `StatCards` and other shared components cannot reflect multi-CC state
- Two parallel execution paths exist with no shared state model

This is the **highest-priority SOLID violation** in the codebase.
**Fix (TD-01):** Add `AppStateService.MultiCCState`, `RunMultiCostCenter()`, and route execution through the service layer.

---

## 4. Production Readiness

### 4.1 Build & CI

| Check | Status | Detail |
|-------|--------|--------|
| Build (0 errors, 0 warnings) | ✅ | Verified 2026-07-08 after CS0436 duplicate-type fix |
| Tests (70/70 pass) | ✅ | `RequestFlowEngineTests` (24) · `AgenticFlowEngineTests` (7) · `MultiCostCenterEngineTests` (20) · `BillingConstantsTests` (10) · `ScenarioPresetsTests` (13) |
| Coverage UBB.Core | ✅ | 87.9% line / 88% branch (threshold: 80%) |
| CI on every push | ✅ | `qa.yml` + `security.yml` in `.github/workflows/` |
| GitHub Pages deployment | ❌ | No `deploy.yml` — publish and gh-pages push is still manual |
| PR status checks | ⚠️ | Workflows exist but no branch protection rules enforcing them |

### 4.2 Coverage Detail

| Class | Line % | Gap / Note |
|-------|--------|------------|
| `RequestFlowEngine` | **100%** | All billing paths covered |
| `ScenarioPresets` | **100%** | All preset keys + outcomes tested |
| `MultiCCPreset` | **100%** | |
| `FlowResult` | **100%** | |
| `RequestFlowState` | **100%** | |
| `RequestPreset` | **100%** | |
| `AgenticStep` | **100%** | |
| `CostCenterBudget` | **80.7%** | `Reset()` method untested |
| `MultiCostCenterState` | **65.3%** | `Reset()` and `AddLog()` paths partially uncovered |
| `BillingConstants` | **69.2%** | `CreditsPerSeat` default branch not tested |
| `CostCenterConfig` | **0%** | Dead model — earmarked for deletion |
| `DailySnapshot` | **0%** | Dead model — earmarked for deletion |
| `SimulationConfig` | **0%** | Dead model — earmarked for deletion |
| `SimulationResult` | **0%** | Dead model — earmarked for deletion |
| `UserConfig` | **0%** | Dead model — earmarked for deletion |

> The 0% classes are **dead code** (TD-05). Deleting them would raise overall coverage and eliminate noise. Without them: effective coverage is ~97% on active code.

### 4.3 Security

| Check | Status | Detail |
|-------|--------|--------|
| Vulnerable NuGet packages | ✅ | Zero CVEs across all 3 projects (2026-07-08) |
| SRI on Bootstrap CSS | ✅ | `integrity="sha384-..."` + `crossorigin="anonymous"` |
| SRI on Bootstrap JS | ✅ | Same |
| XSS — Blazor auto-encoding | ✅ | All `@variable` bindings auto-encoded; no `MarkupString` usage |
| Hardcoded secrets | ✅ | Pure client-side; no API keys, no credentials |
| `UrlStateService.Deserialize` | ⚠️ | Bare `catch {}` silently swallows malformed URL state — no user feedback, no logging |
| Content Security Policy | ❌ | No CSP meta tag or server header. App relies solely on SRI hashes |
| Gitleaks scan | ⚠️ | Configured in CI but requires `secrets.GITHUB_TOKEN` — not testable without a remote |

### 4.4 Accessibility

| Check | Status | Detail |
|-------|--------|--------|
| Mode tabs `role="tablist"` / `role="tab"` / `aria-selected` | ✅ | Correctly implemented in `Home.razor` |
| Form inputs have `<label for>` | ✅ | All inputs labelled |
| Flow diagram nodes | ❌ | Pure `<div>` elements — no `role`, no `aria-label`, no text alternative for colour states |
| Simulation log | ❌ | No `role="log"` or `aria-live="polite"` — screen readers do not announce new results |
| Colour-only state indicators | ⚠️ | Pass/warn/block are green/yellow/red only — no icon or text for colour-blind users |
| Keyboard focus on page load | ✅ | **Fixed this session** — `outline:none` on non-interactive elements prevents the `h1` focus-ring bug |
| `ubb-preset-btn` keyboard nav | ✅ | Uses `<button>` — natively focusable |

### 4.5 Error Handling

| Check | Status | Detail |
|-------|--------|--------|
| Blazor global error UI | ✅ | `blazor-error-ui` div in `index.html` |
| `<ErrorBoundary>` components | ❌ | None — an unhandled exception in `MultiCCPanel` crashes the full page |
| Engine input validation | ⚠️ | `EvaluateStep` accepts negative credits; UI parses with fallback but no user-visible validation message |
| WASM cold-load failure | ❌ | Spinner runs indefinitely; no timeout, no "try refreshing" message |

### 4.6 Performance

| Check | Status | Detail |
|-------|--------|--------|
| Re-render scope | ⚠️ | `State.OnChange` triggers full `Home.razor` re-render (all children) on every input change including `SetField` debounce-less |
| `MultiCCPanel` re-render | ✅ | Local state; explicit `StateHasChanged()` |
| WASM bundle size | ✅ | Minimal deps — only `Microsoft.AspNetCore.Components.WebAssembly` |
| CDN assets | ✅ | Bootstrap loaded from CDN with caching headers |

---

## 5. Documentation Sync Audit

### README.md
| Section | Status | Issue |
|---------|--------|-------|
| Billing model table | ✅ | Accurate |
| Feature bullets | ❌ | Says "6 scenario presets" — now 15 (6 single-user + 9 multi-CC) |
| Single-user scenario table | ✅ | 6 rows, matches `RequestPresets` |
| Multi-CC scenario table | ⚠️ | Shows scenarios 7–15 but numbering doesn't match preset keys; descriptions are approximate |
| Project structure | ❌ | Missing `UBB.Core/`, `tests/`, `MultiCCPanel.razor`, `MultiCCFlowDiagram.razor`, `MultiCCPreset.cs`, `CostCenterBudget.cs`, `MultiCostCenterState.cs` |
| `ScenarioPresets.cs` description | ❌ | Still says "6 preset configurations" |

### plan.md
| Section | Status | Issue |
|---------|--------|-------|
| Phase 3 — `BillingEngine.cs` | ❌ | Describes a full 30-day simulation engine that was never built |
| Phase 4 — Component hierarchy | ❌ | Describes `CostCenterList`, `GlobalSettingsPanel`, `KpiCards` etc. — none exist |
| Multi-CC mode | ❌ | Not mentioned at all |
| Tests / CI | ❌ | Not mentioned at all |

---

## 6. Tech Debt Register (current state)

| ID | Severity | Status | Location | Issue |
|----|----------|--------|----------|-------|
| TD-01 | **High** | **Fixed** | `MultiCCPanel.razor` | Routes through `AppStateService.RunMultiCostCenter()`; subscribes to `OnChange` to sync URL-restored state; all edits call `SetMultiCCState()` |
| TD-02 | **High** | Open | `UrlStateService.cs` | Fully implemented, never wired to UI — dead feature or forgotten Share button |
| TD-03 | **High** | **Fixed** | `plan.md` | Rewritten as accurate as-built architecture doc (solution layout, billing flow, state management, quality gates) |
| TD-04 | Medium | **Fixed** | All node-state dicts | `FlowNode` enum used throughout — no magic string keys anywhere in the codebase |
| TD-05 | Medium | **Fixed** | ~~`DailySnapshot`, `SimulationResult`, `UserConfig`, `CostCenterConfig`~~ | Deleted; `SimulationConfig.CostCenters` property removed; 0% noise eliminated |
| TD-06 | Medium | Open | `BillingConstants.cs` | Promo credit values hardcoded; promo period expires Sept 2026 — no config mechanism |
| TD-07 | Medium | **Fixed** | `MultiCostCenterState.Reset()` | Uses `InitialMeteredBudget` / `InitialPoolRemainingCredits` — restored correctly per-CC and org-level |
| TD-08 | Medium | Open | `README.md` | Feature count, project structure, and preset counts are stale |
| TD-09 | Low | **Fixed** | `Home.razor` URL restore | Shows warning toast when hash is present but neither format can be decoded |
| TD-10 | Low | **Fixed** | `AppStateService` | `SetUserType`, `SetMode`, `Reset` all clear `ActivePresetKey` inline — preset badge correctly invalidated |
| TD-11 | Low | **Fixed** | `app.css` | `outline:none` was scoped to all `div:focus`; now scoped to non-interactive elements |
| TD-12 | Low | **Fixed** | `src/UBB/Models/`, `src/UBB/Services/` | CS0436 duplicate type warnings from files not removed after UBB.Core extraction |
| TD-13 | Low | Open | `MultiCostCenterState.cs` | `Reset()` comment says "Default metered budget" hardcoded at `200_000` — different from the presets |

---

## 7. Prioritised Action Plan

### P1 — Fix Now (build / correctness)
*(Build is currently clean — these are the next highest-risk items)*
1. **TD-10** — `SetUserType` / `SetMode` do not clear `ActivePresetKey`; preset badge shows stale state
2. **TD-07** — `Reset()` on `MultiCostCenterState` uses hardcoded budget value
3. **TD-09** — `UrlStateService` bare catch; at minimum log to browser console

### P2 — Fix Before Next Feature (SOLID / architecture)
4. ~~**TD-01**~~ — Fixed: multi-CC now routes through `AppStateService`; subscribes to `OnChange`; all edits publish state
5. ~~**TD-04**~~ — Already fixed: `FlowNode` enum used everywhere
6. **TD-02** — Wire `UrlStateService` to a Share button or delete it

### P3 — Clean-Up Sprint
7. ~~**TD-05**~~ — Fixed: deleted `DailySnapshot`, `SimulationResult`, `UserConfig`, `CostCenterConfig`; removed `SimulationConfig.CostCenters`
8. ~~**TD-03**~~ — Fixed: `plan.md` rewritten as accurate as-built architecture doc
9. **TD-08** — Update README features list, project structure table, preset count

### P4 — Before Public / Production Release
10. **TD-06** — Move billing constants to `wwwroot/billing-config.json` (Factor III)
11. **Accessibility** — `role="log"` + `aria-live` on execution log; `aria-label` + colour+icon on flow nodes
12. **Error boundaries** — `<ErrorBoundary>` around `MultiCCPanel` and results section
13. **Deploy workflow** — Add `.github/workflows/deploy.yml` for GitHub Pages (Factor V)
14. **CSP** — Meta `Content-Security-Policy` tag (A05)

### P5 — Nice to Have
15. **OCP** — `ISimulationMode` interface for extensible mode system
16. **Factor XI** — `SimulationLogEntry` record with `Level` + `Timestamp`
17. **Performance** — `ShouldRender()` overrides on leaf components
18. **CDN vendoring** — Move Bootstrap to `wwwroot/lib/` to eliminate CDN dependency at runtime
