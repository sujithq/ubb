# Copilot Instructions — UBB Simulator

## Repository overview
Blazor WebAssembly (.NET 10) simulator for GitHub Copilot usage-based billing governance.
Pure client-side SPA: all billing logic runs in-browser via C#, no backend.

**Solution layout**
```
src/UBB.Core/     — pure C# library: Models + RequestFlowEngine + BillingConstants + ScenarioPresets
src/UBB/          — Blazor WASM app (UI only; references UBB.Core)
tests/UBB.Tests/  — xUnit + FluentAssertions unit tests (references UBB.Core only)
tests/UBB.E2E/    — Playwright E2E tests (URL sharing, state restoration, mode switching)
```

---

## Non-negotiable rules

### After every code change you must:
1. Invoke the **`#qa` agent** — build must be clean, all tests must pass, coverage ≥ 80%.
2. For any change touching security-relevant files (`index.html`, `404.html`, `*.csproj`, `catch` blocks, CDN links), also invoke the **`#security` agent**.
3. **For URL sharing changes (TD-02, TD-03)**: Run E2E tests locally with `./run-e2e-tests.ps1` (or `.sh` on macOS/Linux) to verify across all simulation modes.

Agents are defined in `.github/agents/`. Do not consider a change complete until both relevant agents report green. E2E tests must pass before pushing.

---

## Architecture constraints

- **Engine logic belongs in `UBB.Core`**, not in Razor components or `AppStateService`.
- **All simulation execution routes through `AppStateService`**. Components must not call `RequestFlowEngine` directly.
- **No magic string node-state keys** in new code. Use the existing string constants or add to `FlowResult.DefaultNodeStates()`.
- **`UrlStateService`** is registered but not yet wired to UI. Do not remove it; wire it to a Share button when implementing.
- **Dead models deleted** — `DailySnapshot`, `SimulationResult`, `UserConfig`, `CostCenterConfig` have been removed from `UBB.Core/Models/`. Do not re-add them.

---

## Coding standards

- **C# nullable reference types** are enabled — no `!` null-forgiving operators without justification.
- **No hardcoded billing constants** outside `BillingConstants.cs`.
- **No raw `catch {}` blocks** that swallow exceptions silently.
- All new public methods in `UBB.Core` must have at least one corresponding unit test.
- Razor components: use `[Parameter, EditorRequired]` on required parameters.
- Prefer `multi_replace_string_in_file` for multiple simultaneous edits over sequential single edits.

---

## Testing rules

- Tests live in `tests/UBB.Tests/`.
- Test class names: `{Subject}Tests` (e.g. `RequestFlowEngineTests`).
- Test method names: `{Method}_{Condition}_{ExpectedResult}` (e.g. `ULB_WhenRequestExceedsLimit_Blocks`).
- Use **FluentAssertions** — never `Assert.Equal` / `Assert.True`.
- Tests must be deterministic — no `DateTime.Now`, no `Random`, no file I/O.
- When writing metered-phase tests (CC/Enterprise checks), set `userLimit` high enough to bypass ULB. Document why in a comment.

---

## Security rules (OWASP Top 10 awareness)

- **A05 — Security misconfiguration**: Bootstrap is vendored locally (`wwwroot/lib/bootstrap/`) — there are no runtime CDN dependencies. If a CDN `<script>`/`<link>` is ever (re)introduced it must have `integrity` (SRI) and `crossorigin` attributes. The CSP in `index.html`/`404.html` is `'self'`-only for scripts — keep it that way.
- **A03 — Injection**: Blazor renders `@variable` with automatic HTML encoding — maintain this; never use `MarkupString` on user-provided data.
- **A06 — Vulnerable components**: Run `dotnet list package --vulnerable` before committing. Zero tolerance for known CVEs.
- **A08 — Integrity failures**: Do not add new CDN dependencies without SRI hashes.
- No secrets, API keys, or credentials in source. The app is purely client-side.

---

## CI gates (automated — run on every push and PR)

| Workflow | File | What it checks |
|----------|------|----------------|
| **QA** | `.github/workflows/qa.yml` | Build (warnings as errors) · Tests (141 unit tests) · Coverage ≥ 80% (UBB.Core) |
| **Security** | `.github/workflows/security.yml` | Vulnerable NuGet packages · Roslyn analyzers · SRI integrity audit · Gitleaks secret scan |
| **E2E** | `.github/workflows/e2e-tests.yml` | URL sharing across all modes (Single, Agentic, Multi-CC) · Preset preservation · State restoration · URL format validation |

All three workflows **must be green** before merging to `main`.
