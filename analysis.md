# UBB Simulator — Deep Analysis Working Document
*Generated: 2026-07-08 | Scope: production-readiness, 12-factor, SOLID, docs, tech debt*

---

## 1. Executive Summary

| Dimension | Score | Notes |
|-----------|-------|-------|
| 12-Factor compliance | 6 / 12 | Several factors are N/A for static WASM; gaps in III, XI, XII |
| SOLID adherence | B+ | SRP/OCP/DIP strong; LSP gap in node-state dictionaries; ISP partial |
| Production readiness | 60% | Missing error boundaries, accessibility gaps, no CSP, no versioning |
| Docs / code sync | ~75% | README scenarios table already out of sync (9 presets documented, README shows 15 rows with wrong numbering); `plan.md` describes a future BillingEngine never built |
| Tech debt | Medium | Dead models, dual billing paths, magic string node keys, `UrlStateService` never wired to UI |

---

## 2. 12-Factor Analysis

> A Blazor WASM app is a **static client-side SPA** — it has no server process. Factors I, II, V, IX, X, XI apply fully. Factors VI, VII, VIII are trivially satisfied or N/A. Factor IV is N/A (no backing services). Factors III and XII require deliberate design even in frontend apps.

### Factor I — Codebase ✅
One repo, one app, Git-tracked. No multi-app sprawl.
**Gap:** `plan.md` describes a `BillingEngine.cs` and `CostCenterList.razor` that were never built — the plan is stale and misleading. Should either be updated or deleted.

### Factor II — Dependencies ✅
`UBB.csproj` pins exact package versions (`10.0.9`). Bootstrap/Chart.js pulled from CDN with SRI hashes (integrity attributes present in `index.html`).
**Gap:** CDN availability is a runtime dependency not expressed in the project file. If `cdn.jsdelivr.net` is down the app loads blank. Consider vendoring CSS/JS into `wwwroot` for a production deployment.

### Factor III — Config ⚠️
All "configuration" (billing constants, promo dates, default budgets) is **hardcoded** in C# source:
- `BillingConstants.cs`: credit prices, seat costs — these change when GitHub updates pricing.
- `ScenarioPresets.cs`: 9 multi-CC presets with hardcoded credit numbers.
- `RequestFlowState.cs`: default values (ULB 2500, pool 390000, etc.) baked into property initialisers.
- The promo period (`BusinessPromoCredits = 3_000`) ends Sept 2026 — no feature flag or config override mechanism exists.
**Fix:** Extract pricing into `appsettings.json` or a `BillingConfig` loaded at startup via `HttpClient` from a static JSON file. For WASM: `wwwroot/config.json` fetched in `Program.cs`.

### Factor IV — Backing Services N/A
No databases, queues, or external APIs. All computation is in-browser.
**Note:** `UrlStateService` exists and serialises `SimulationConfig` to a URL fragment, but **it is never called from any component or page**. This feature is dead code.

### Factor V — Build, Release, Run ✅
`dotnet publish` produces a deterministic static bundle. The `wwwroot/404.html` handles GitHub Pages SPA routing. `README.md` documents the publish command.
**Gap:** No CI pipeline exists (no `.github/workflows/`). There is no automated build-on-push or deploy-to-Pages step.

### Factor VI — Processes ✅ (trivially)
WASM runs as a single browser process. All state is in-memory (singleton `AppStateService`). State resets on page reload by design.

### Factor VII — Port Binding ✅ (N/A)
Static files served by any HTTP server. No port binding concern.

### Factor VIII — Concurrency ✅ (N/A)
Single-tab, single-user SPA. No concurrency model required.

### Factor IX — Disposability ⚠️
`Home.razor` correctly implements `IDisposable` and unsubscribes from `State.OnChange`.
**Gap:** `MultiCCPanel.razor` holds `CurrentState` as local component state but has no cleanup. If the component is conditionally rendered and re-created, the old state leaks until GC (minor in WASM, but the pattern is fragile).
**Gap:** No loading state management — if WASM bundle is slow on a cold load, the spinner shows indefinitely with no timeout or error message.

### Factor X — Dev/Prod Parity ✅
No environment-specific code paths. `<base href="/">` is correctly set. The only delta between dev and prod is the CDN availability (see Factor II).

### Factor XI — Logs ❌
Logs are stored as `List<string>` inside `RequestFlowState` and `MultiCostCenterState` — they are **UI strings, not structured events**. There is no log level, no timestamp on the single-user flow logs (multi-CC has timestamps via `AddLog`, single-user does not), no way to export or persist logs.
**Fix:** Introduce a `SimulationLogEntry` record with `Level`, `Timestamp`, `Message`. The execution log component renders from this. This also enables filtering by severity.

### Factor XII — Admin Processes ❌
No mechanism to update billing constants (e.g., when GitHub changes the promotional credit period from 3,000 to a new value) without a code change and redeploy. No seed/migration concept.
**Fix:** `wwwroot/billing-config.json` with versioned constants fetched at startup.

---

## 3. SOLID Principles

### S — Single Responsibility ✅ (mostly)
| Class | Verdict | Note |
|-------|---------|------|
| `RequestFlowEngine` | ✅ | Pure stateless evaluation; two public methods `EvaluateStep` + `RunAgentic` + `RunMultiCostCenter` |
| `AppStateService` | ⚠️ | Manages state AND triggers re-renders AND applies presets AND calls engine. Consider splitting: `SimulationRunner` extracts engine calls |
| `Home.razor` | ⚠️ | 240+ line component doing layout, input binding, mode switching, field parsing. The `SetField` + `BtnClass` helpers are fine but the page is doing too much |
| `MultiCCPanel.razor` | ⚠️ | Configuration UI, preset selection, simulation execution, and result rendering all in one component (~240 lines) |
| `ScenarioPresets.cs` | ✅ | Data only |
| `BillingConstants.cs` | ✅ | Constants only |

### O — Open/Closed ⚠️
Adding a new simulation mode (e.g., "Monthly 30-day") requires:
- Adding to `SimulationMode` enum ✅
- Modifying `Home.razor` (mode tab, conditional rendering) ❌
- Modifying `AppStateService.Run()` dispatch ❌
- Adding a new panel component ✅

**Fix:** A `ISimulationMode` interface (or abstract record) with `Render()` and `Run()` would let new modes be added without touching existing code.

### L — Liskov Substitution ⚠️
Node state dictionaries (`Dictionary<string, FlowNodeState>`) use magic string keys (`"pool"`, `"costCentre"`, `"enterprise"`). The single-user flow uses 6 keys; the multi-CC flow uses 5 (no `"user"` key). Consumers call `.TryGetValue` defensively but the contract is implicit.
**Fix:** Replace string keys with a `FlowNode` enum: `User, Pool, Paid, CostCentre, Enterprise, Result`. Compile-time safety, no magic strings.

### I — Interface Segregation ⚠️
`AppStateService` is injected into `Home.razor` as a single fat service. Components that only need to read state (`StatCards`, `FlowDiagram`) still take a dependency on the full service via `RequestFlowState` parameter passing — this is actually handled correctly via `[Parameter]`.
`MultiCCPanel` calls `RequestFlowEngine` directly (static class), bypassing `AppStateService`. This creates two parallel execution paths with no shared state.

### D — Dependency Inversion ⚠️
`MultiCCPanel.razor` calls `RequestFlowEngine.RunMultiCostCenter()` directly — tight coupling to a concrete static class. `Home.razor` calls `State.RunSingle()` / `State.RunAgentic()` through `AppStateService` — correctly abstracted.
Both patterns coexist: multi-CC bypasses the service layer.
**Fix:** Route multi-CC execution through `AppStateService.RunMultiCostCenter()` like the other modes.

---

## 4. Production Readiness

### 4.1 Security
| Check | Status | Detail |
|-------|--------|--------|
| SRI hashes on CDN | ✅ | Bootstrap CSS + JS both have `integrity` attributes |
| CSP header | ❌ | No Content-Security-Policy. Serving from GitHub Pages provides no server-side headers. A `_headers` file (for Netlify/CF Pages) or meta CSP tag is missing |
| XSS in log rendering | ✅ | Logs rendered via `@log` (Blazor auto-encodes) |
| URL hash deserialization | ⚠️ | `UrlStateService.Deserialize` has a bare `catch {}` swallowing all exceptions silently. Should log or surface parse errors |
| No secrets | ✅ | Pure client-side, no API keys |

### 4.2 Accessibility
| Check | Status | Detail |
|-------|--------|--------|
| Mode tabs have `role="tablist"` / `role="tab"` | ✅ | |
| `aria-selected` on mode tabs | ✅ | |
| Flow diagram nodes | ❌ | Pure `<div>` elements with no `role`, no `aria-label`. Screen readers get nothing |
| Simulation log | ❌ | No `role="log"` or `aria-live="polite"` — screen readers won't announce new results |
| Colour-only state indicators | ⚠️ | Pass/warn/block distinguished by colour only (green/yellow/red). No icon or text for colour-blind users in flow nodes |
| Input labels | ✅ | All inputs have associated `<label for>` |
| Keyboard navigation | ⚠️ | `ubb-preset-btn` is a `<button>` ✅, but multi-CC preset buttons use Bootstrap `btn` classes — focusable but no `:focus-visible` override in CSS |

### 4.3 Error Handling
| Check | Status | Detail |
|-------|--------|--------|
| Blazor error UI | ✅ | `blazor-error-ui` div in `index.html` |
| Error boundaries | ❌ | No `<ErrorBoundary>` component wrapping panels. An exception in `MultiCCPanel` will crash the whole page |
| Engine input validation | ⚠️ | `EvaluateStep` accepts negative credits without error. `MultiCCPanel` `UpdateCCBudget` parses with fallback `"0"` but no user feedback |
| WASM load failure | ❌ | Spinner shows indefinitely if WASM fails to load. No fallback message or timeout |

### 4.4 Performance
| Check | Status | Detail |
|-------|--------|--------|
| Re-render scope | ⚠️ | `AppStateService.OnChange` triggers `StateHasChanged()` in `Home.razor` which re-renders the entire page subtree. `StatCards`, `FlowDiagram`, `ExecutionLog` all re-render on every change |
| `MultiCCPanel` re-render | ✅ | Local state, `StateHasChanged()` called explicitly |
| Large preset list | ✅ | 9 presets — no virtualisation needed |
| WASM bundle size | ✅ | Minimal dependencies; only `Microsoft.AspNetCore.Components.WebAssembly` |

---

## 5. Documentation Sync Audit

### README.md
| Section | Status | Issue |
|---------|--------|-------|
| Billing model table | ✅ | Accurate |
| Single-user scenarios table | ✅ | 6 rows, matches `ScenarioPresets.RequestPresets` |
| Multi-CC scenarios table | ❌ | Shows scenarios 7–15 (9 rows) but uses different numbering convention and descriptions than the actual preset labels |
| Features list | ❌ | Still says "6 scenario presets" — now 6 single-user + 9 multi-CC = 15 total |
| Project structure | ❌ | Missing: `MultiCCPanel.razor`, `MultiCCFlowDiagram.razor`, `MultiCCPreset.cs`, `CostCenterBudget.cs`, `MultiCostCenterState.cs` |
| `ScenarioPresets.cs` description | ❌ | Says "6 preset configurations" — now 15 |

### plan.md
| Section | Status | Issue |
|---------|--------|-------|
| Phase 3 — `BillingEngine.cs` | ❌ | Describes a full 30-day daily simulation engine — never built. `RequestFlowEngine.cs` is a different, simpler implementation |
| Phase 4 — Component hierarchy | ❌ | Describes `CostCenterList`, `GlobalSettingsPanel`, `ScenarioPresetBar` — none exist |
| Multi-CC scenarios | ❌ | Not mentioned (added later) |

### Code comments
| File | Status | Issue |
|------|--------|-------|
| `RequestFlowEngine.cs` | ✅ | Header comment accurate |
| `MultiCostCenterState.cs` | ⚠️ | `Reset()` hardcodes `200_000` as default metered budget, inconsistent with `CreateDefault()` which uses different per-CC values |
| `BillingConstants.cs` comment | ⚠️ | `UsePromotionalCredits` comment says "June–Sept 2026" — this expires in ~2 months |
| `UrlStateService.cs` | ✅ | Accurate, but the service is never used |

---

## 6. Tech Debt Register

| ID | Severity | Location | Issue | Suggested Fix |
|----|----------|----------|-------|---------------|
| TD-01 | High | `MultiCCPanel.razor` | Calls `RequestFlowEngine` directly, bypassing `AppStateService`. Dual execution paths | Route through `AppStateService.RunMultiCostCenter()` |
| TD-02 | High | `UrlStateService.cs` | Fully implemented but never wired to any UI | Wire to a "Share" button in the header, or delete |
| TD-03 | High | `plan.md` | Describes architecture that was never built (BillingEngine, 30-day simulation) | Update to reflect actual implementation |
| TD-04 | Medium | All node-state dicts | Magic string keys `"pool"`, `"costCentre"` etc. — no compile-time safety | Replace with `FlowNode` enum |
| TD-05 | Medium | `DailySnapshot.cs`, `SimulationResult.cs`, `UserConfig.cs`, `CostCenterConfig.cs` | Models for the 30-day simulation engine that was never built — dead code | Delete or move to a `planned/` folder |
| TD-06 | Medium | `BillingConstants.cs` | Promo credit values hardcoded; promo period expires Sept 2026 | Move to `wwwroot/billing-config.json` |
| TD-07 | Medium | `MultiCostCenterState.Reset()` | Hardcodes `200_000` as default CC budget, ignoring actual configured values | Reset to initial `MeteredRemainingCredits` stored at CC creation |
| TD-08 | Medium | `README.md` | Features list, project structure, and preset counts are stale | Update counts and add new components |
| TD-09 | Low | `RequestFlowEngine.RunMultiCostCenter` | 2000 credits was previously hardcoded; now configurable via `RequestCreditsPerCC`, but the comment `// Simulate each CC making a request` gives no context on what "a request" means | Add a doc comment explaining the model |
| TD-10 | Low | `Home.razor` | `SetField` generic helper clears `ActivePresetKey` — but user-type buttons (`SetUserType`) and Reset do not clear preset key consistently | Ensure all state mutations clear preset tracking |
| TD-11 | Low | `MultiCCFlowDiagram.razor` | `Fmt(Cc.MeteredRemainingCredits)` passes `int` as `decimal` parameter — works but inconsistent | Use `int` consistently or normalise to `decimal` in the model |
| TD-12 | Low | `app.css` | `outline: none` suppression applied to all `div:focus` — overly broad; removes outline from any focusable div including Bootstrap components | Scope to `.ubb-hero h1:focus` only |

---

## 7. Prioritised Action Plan

### P1 — Fix Now (correctness / user trust)
1. **TD-01**: Route multi-CC execution through `AppStateService` — single execution path
2. **TD-07**: Fix `Reset()` to use actual CC budgets
3. **TD-12**: Scope `outline:none` to the hero heading only
4. **README**: Fix feature count + project structure table

### P2 — Fix Soon (maintainability)
5. **TD-04**: Introduce `FlowNode` enum, eliminate magic strings
6. **TD-02**: Wire `UrlStateService` to a Share button or remove it
7. **TD-03**: Rewrite `plan.md` as an accurate architecture doc
8. **Factor XI**: Add `SimulationLogEntry` record with `Level` + `Timestamp`

### P3 — Fix Before Public Release
9. **Factor III**: Move billing constants to `wwwroot/billing-config.json`
10. **Factor V**: Add `.github/workflows/deploy.yml` for CI/CD to GitHub Pages
11. **Accessibility**: Add `role="log"` + `aria-live` to execution log; add colour+icon indicators to flow nodes; add `aria-label` to nodes
12. **Error boundaries**: Wrap `MultiCCPanel` and `Home.razor` content in `<ErrorBoundary>`
13. **TD-05**: Delete unused model classes (`DailySnapshot`, `SimulationResult`, `UserConfig`, `CostCenterConfig`)

### P4 — Nice to Have
14. **OCP refactor**: `ISimulationMode` to make adding new modes non-breaking
15. **Performance**: Implement `ShouldRender()` overrides on leaf components to reduce re-render cascade
16. **CSP**: Add meta CSP tag or serve via platform with header support
