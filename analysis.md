# UBB Simulator — Deep Analysis Working Document
*Generated: 2026-07-09 | Fresh analysis after full TD-01→TD-13 closure, P4 hardening, and P5 disposition*

---

## 0. What Changed Since Last Analysis (2026-07-08)

| Area | Before | Now |
|------|--------|-----|
| Tech debt register | TD-01…TD-13 open/partial | **All closed** (fixed or consciously dispositioned) |
| Unit tests | 70 → 138 | **145** (added URL bad-input, Reset semantics, AppState serialization) |
| Coverage (UBB.Core) | 87.9% line | **99.1% line** (dead models deleted + new tests) |
| E2E tests | None | **6 Playwright tests** (URL sharing across all 3 modes) + `e2e-tests.yml` CI |
| URL sharing | `UrlStateService` unwired | Share button + full app-state restore (flow, multi-CC, preset, mode) with legacy fallback + error toast |
| Multi-CC state | Component-local, bypassed `AppStateService` | Routed through `AppStateService`; `OnChange` sync; URL-restorable |
| Node-state keys | Magic strings | `FlowNode` enum everywhere |
| Dead code | 4 dead models, Counter/Weather/NavMenu, stale Bootstrap 5.3.3 (~3.5 MB), weather.json | **All deleted** |
| Bootstrap | CDN 5.3.8 with SRI | **Vendored locally** (`wwwroot/lib/bootstrap/`), hash-verified against previous SRI pins |
| CSP | None | `'self'`-only `script-src` meta in `index.html` + `404.html`, CI-enforced |
| Error boundaries | None | `<ErrorBoundary>` around `MultiCCPanel` and `FlowDiagram` |
| Accessibility | Colour-only flow nodes | `role="group"` + `aria-label` + ✓/⚠/✕ icons; log already had `role="log"` + `aria-live` |
| Deployment | Manual | `deploy.yml` → GitHub Pages with `<base href>` rewrite |
| XSS surface | `showToast` used `innerHTML` interpolation | Fixed — `textContent` DOM construction; helpers externalized to `js/ubb.js` |
| Docs | README/plan.md stale | Synced (plan.md rewritten as-built; README structure/features/testing updated) |

---

## 1. Executive Summary

| Dimension | Score | Trend | Notes |
|-----------|-------|-------|-------|
| 12-Factor compliance | 9 / 12 applicable | ↑ | Vendored deps, deploy pipeline, CSP; config externalization consciously declined |
| SOLID adherence | A− | ↑ | DIP fixed (TD-01), LSP fixed (FlowNode enum); only residual SRP pressure in `Home.razor` |
| Production readiness | ~90% | ↑ | All gates automated; remaining gaps are minor (load-failure fallback, branch protection) |
| Docs / code sync | 95% | ↑ | plan.md as-built, README current; only this document ages |
| Tech debt | Low | ↓ | Register cleared; 5 new low-severity findings below (TD-14…TD-18) |

---

## 2. 12-Factor Analysis

> Factors IV (backing services), VI (processes), VII (port binding), VIII (concurrency) are N/A for a static WASM SPA.

### Factor I — Codebase ✅
One repo, 4 projects (`UBB.Core`, `UBB`, `UBB.Tests`, `UBB.E2E`), no circular references. `plan.md` now accurately describes the as-built system.

### Factor II — Dependencies ✅ (improved)
NuGet pinned; **zero runtime CDN dependencies** — Bootstrap 5.3.8 vendored at `wwwroot/lib/bootstrap/`, downloads hash-verified against the previously pinned SRI values. App loads in air-gapped/CDN-blocked environments. Zero CVEs (verified 2026-07-09).

### Factor III — Config ⚠️ (accepted)
Billing constants remain hardcoded. **Consciously dispositioned** (ex-TD-06): only `CreditValueDollars` is consumed at runtime (one log line); seat costs / promo credits are test-only + doc text. A JSON-config pipeline for one constant is over-engineering. Revisit if seat-cost simulation is built.

### Factor V — Build, Release, Run ✅ (completed)
Four CI workflows: `qa.yml` (build + tests + coverage ≥ 80%), `security.yml` (CVEs, SRI + CSP audit, Gitleaks), `e2e-tests.yml` (Playwright), `deploy.yml` (GitHub Pages with automatic `<base href>` rewrite).
**Remaining gap:** branch protection rules requiring green checks cannot be verified locally — confirm on the remote.

### Factor IX — Disposability ⚠️
`Home.razor` and `MultiCCPanel` correctly implement `IDisposable`/unsubscribe.
**Remaining gap (TD-16):** WASM cold-load failure still shows an indefinite spinner — no timeout or "try refreshing" fallback.

### Factor X — Dev/Prod Parity ✅
No environment-specific code paths; `deploy.yml` rewrites `<base href>` at publish time rather than requiring divergent source.

### Factor XI — Logs ⚠️ (accepted)
`List<string>` with `PASS`/`WARN`/`BLOCK` prefixes (drives log colouring); multi-CC entries carry timestamps. Structured `SimulationLogEntry` was **dispositioned**: it would break the JSON shape of previously shared URLs for an unrequested filtering feature.
**New note (TD-14):** `MultiCostCenterState.AddLog` uses `DateTime.Now` inside `UBB.Core` — a purity smell in the domain library (tests must avoid asserting timestamps).

### Factor XII — Admin Processes ⚠️ (accepted risk)
No runtime config update mechanism; follows from the Factor III disposition. Promo-credit doc text will need a source edit after Sept 2026.

---

## 3. SOLID Principles

### S — Single Responsibility ⚠️
| Class / Component | Lines | Verdict |
|-------------------|-------|---------|
| `RequestFlowEngine` | 193 | ✅ Pure stateless evaluation |
| `Home.razor` | **347** | ⚠️ Grew +100 lines this cycle (share button, URL restore, error boundaries). Extraction candidates: URL-restore logic → a `UrlRestoreService`/code-behind; controls column → `ControlsPanel.razor` (TD-15) |
| `MultiCCPanel.razor` | 267 | ⚠️ Config UI + sync + execution + rendering; manageable but at the ceiling |
| `AppStateService` | 136 | ✅ Acceptable — state + runs + notifications, cohesive |
| `UrlStateService` | 124 | ⚠️ Contains a production-dead legacy path (TD-17) |

### O — Open/Closed ✅ (dispositioned)
Enum-switch mode dispatch retained deliberately: 3 modes, colocated, compile-time-checked. `ISimulationMode` was assessed as speculative generality.

### L — Liskov Substitution ✅ (fixed)
`Dictionary<FlowNode, FlowNodeState>` everywhere — magic strings eliminated. Single-flow uses 6 nodes, per-CC diagrams 5 (no `User`); the difference is now visible in the type usage rather than implicit strings.

### I — Interface Segregation ✅
Read-only components receive state via `[Parameter]` (`StatCards` takes `FlowState` + optional `MultiCCState`); `PresetPanel` uses `EventCallback`.

### D — Dependency Inversion ✅ (fixed — was the top violation)
`MultiCCPanel` now routes everything through `AppStateService` (`SetMultiCCState`, `RunMultiCostCenter`), subscribes to `OnChange`, and adopts URL-restored state via `SyncFromAppState()`. Single execution path; multi-CC state is shareable and observable app-wide.

---

## 4. Production Readiness

### 4.1 Build & CI
| Check | Status | Detail |
|-------|--------|--------|
| Build (0 warnings, `-warnaserror`) | ✅ | Verified 2026-07-09 |
| Unit tests | ✅ | 145/145 across Engine, Models, Presets, Services, Components (bUnit) |
| Coverage UBB.Core | ✅ | **99.1% line** (420/424) — threshold 80% |
| E2E | ✅ | 6/6 Playwright (URL sharing, all modes, CSP-validated) |
| CI on push/PR | ✅ | qa · security · e2e-tests; deploy on `main` |
| E2E CI reliability | ✅ | Fixed 2026-07-09: lockfile committed for `npm ci`, `--with-deps chromium`, debug log untracked |
| Branch protection | ❓ | Cannot verify locally — check remote settings |

### 4.2 Security
| Check | Status | Detail |
|-------|--------|--------|
| Vulnerable NuGet packages | ✅ | Zero CVEs (2026-07-09) |
| CDN dependencies | ✅ | **None** — Bootstrap vendored, hash-verified |
| CSP | ✅ | `'self'`-only `script-src` (+ `wasm-unsafe-eval`) in both HTML files; CI-enforced regression guard in `security.yml` |
| XSS | ✅ | Blazor auto-encoding; `showToast` innerHTML sink fixed (`textContent`); no `MarkupString` on user data |
| URL-state tampering | ✅ | Malformed hashes: logged to console, warning toast shown, safe default state (covered by 6 bad-input unit tests) |
| Secrets | ✅ | None; pure client-side |

### 4.3 Accessibility
| Check | Status | Detail |
|-------|--------|--------|
| Mode tabs (`role="tab"`, `aria-selected`) | ✅ | |
| Form labels | ✅ | All inputs labelled |
| Flow diagram nodes | ✅ | `role="group"` + `aria-label` with state text; ✓/⚠/✕ icons (not colour-only) |
| Execution log | ✅ | `role="log"` + `aria-live="polite"` |
| Multi-CC diagrams | ✅ | Text state labels per node |
| Keyboard navigation | ✅ | Native `<button>` elements throughout |

### 4.4 Error Handling
| Check | Status | Detail |
|-------|--------|--------|
| Blazor global error UI | ✅ | `blazor-error-ui` div |
| `<ErrorBoundary>` | ✅ | Around `MultiCCPanel` and `FlowDiagram` with friendly fallbacks |
| URL restore failure | ✅ | Warning toast + safe defaults |
| WASM cold-load failure | ❌ | Indefinite spinner — no timeout/fallback (TD-16) |
| Engine input validation | ⚠️ | UI clamps to ≥ 0; engine itself accepts negatives (UI is the only caller — acceptable) |

---

## 5. Documentation Sync

| Document | Status |
|----------|--------|
| `plan.md` | ✅ Rewritten as as-built architecture (2026-07-09) |
| `README.md` | ✅ Features (15 presets, multi-CC, URL sharing), structure, port 5295, testing section |
| `.github/copilot-instructions.md` | ✅ Vendored-Bootstrap invariant, count-free CI table, deploy row |
| `.github/agents/qa.md` / `security.md` | ✅ Count-free; security checks reflect vendored + CSP-mandatory state |

---

## 6. Tech Debt Register (fresh — previous TD-01…TD-13 all closed, see git history)

| ID | Severity | Status | Location | Issue |
|----|----------|--------|----------|-------|
| TD-14 | Low | Open | `MultiCostCenterState.AddLog` | `DateTime.Now` inside the pure domain library — injectable clock or caller-supplied timestamp would restore purity and full test determinism |
| TD-15 | Medium | Open | `Home.razor` (347 lines) | SRP pressure: URL-restore logic and the controls column are extraction candidates before the next feature lands |
| TD-16 | Low | Open | `index.html` | WASM cold-load failure leaves an indefinite spinner — add a JS timeout that swaps in a "reload" message |
| TD-17 | Low | Open | `UrlStateService` | Legacy `Serialize(SimulationConfig)` / `Deserialize` / `PushToUrl` are production-dead (test-only); `SimulationConfig` model exists solely for them. Note: the *actual* legacy share-URL fallback is `DeserializeFlowState` — that one must stay |
| TD-18 | Low | Open | `tests/UBB.E2E` | E2E covers URL sharing only — no E2E for Run/preset/mode-switch happy paths (unit/bUnit tests cover the logic, so risk is low) |
| TD-19 | Info | Noted | `MultiCCPanel` | Every field edit calls `SetMultiCCState` → `Notify()` → app-wide re-render. Correct and required for URL freshness; revisit only if input latency is ever observed |

---

## 7. Prioritised Action Plan

### P1 — Before next feature
1. **TD-15** — Extract `Home.razor` URL-restore logic (and optionally the controls column) — largest file, still growing
2. **TD-17** — Delete the production-dead `SimulationConfig` serialize path + its tests (keep `DeserializeFlowState` legacy fallback)

### P2 — Opportunistic
3. **TD-14** — Timestamp injection for `AddLog`
4. **TD-16** — Cold-load timeout fallback in `index.html`
5. **TD-18** — One E2E happy-path test per mode (run + assert result node)

### P3 — Verify on remote (not doable locally)
6. Branch protection: require QA + Security + E2E green before merge to `main`
7. Confirm GitHub Pages is configured for `deploy.yml` (Settings → Pages → GitHub Actions source); set `PAGES_ROOT=true` repo variable if deploying to a user/org root site
