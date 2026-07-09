# Architecture — UBB Simulator
## GitHub Copilot Usage-Based Billing Simulator (Blazor WASM)

> This document describes the architecture **as built**. Last synced: 2026-07-09.
> An earlier version of this file described a 30-day billing engine (`BillingEngine.cs`) that was never implemented — that design was superseded by the request-flow model below.

---

## TL;DR

Single-page Blazor WebAssembly (.NET 10) app. All billing logic runs client-side in C# — no backend. Simulates how a single AI request (or agentic workflow, or multiple competing cost centers) flows through GitHub Copilot's usage-based-billing guardrails: user-level budget → shared pool → cost-centre metered budget → enterprise metered cap. State is shareable via a Base64URL-encoded URL fragment. Static output deployable to GitHub Pages.

---

## Solution layout

```
UBB.sln
├── src/UBB.Core/          Pure C# domain library — no Blazor dependencies
│   ├── Models/
│   │   ├── RequestFlowState.cs       Single/agentic simulation state (serializable)
│   │   ├── MultiCostCenterState.cs   Multi-CC simulation state + Reset() semantics
│   │   ├── CostCenterBudget.cs       Per-CC budget, consumption, node states
│   │   ├── FlowResult.cs             Engine output: logs + node states + balances
│   │   ├── FlowNode.cs               Enum: User, Pool, Paid, CostCentre, Enterprise, Result
│   │   ├── FlowNodeState.cs          Enum: Idle, Pass, Warn, Block
│   │   ├── AgenticStep.cs            One step of the agentic workflow
│   │   ├── RequestPreset.cs          Single-user scenario preset data
│   │   ├── MultiCCPreset.cs          Multi-CC scenario preset data
│   │   ├── SimulationConfig.cs       Legacy URL-sharing config (seats, toggles)
│   │   ├── Enums.cs                  UserType, SimulationMode
│   │   └── LicenseType.cs            Business / Enterprise
│   └── Services/
│       ├── RequestFlowEngine.cs      Stateless evaluation engine (single, agentic, multi-CC)
│       ├── BillingConstants.cs       Credit price, seat costs, promo credit amounts
│       └── ScenarioPresets.cs        6 single-user + 9 multi-CC presets
│
├── src/UBB/               Blazor WASM app (UI only; references UBB.Core)
│   ├── Pages/Home.razor              Main page: controls, mode tabs, share button, URL restore
│   ├── Components/
│   │   ├── FlowDiagram.razor         Single-flow node diagram (6 nodes)
│   │   ├── MultiCCFlowDiagram.razor  Per-CC node diagram (5 nodes)
│   │   ├── MultiCCPanel.razor        Multi-CC config + run; syncs with AppStateService
│   │   ├── PresetPanel.razor         Single-user preset buttons with active highlight
│   │   ├── StatCards.razor           Credit balance stat cards
│   │   ├── ExecutionLog.razor        Simulation log output
│   │   ├── AgenticWorkflow.razor     Agentic step editor
│   │   └── InfoTip.razor             Bootstrap popover help icons with docs links
│   └── Services/
│       ├── AppStateService.cs        Single source of truth; OnChange event; all runs route here
│       └── UrlStateService.cs        Base64URL (de)serialization for share links
│
├── tests/UBB.Tests/       xUnit + FluentAssertions + bUnit (145 tests)
│   ├── Engine/                       RequestFlowEngine, Agentic, MultiCC, BillingConstants
│   ├── Models/                       CostCenterBudget, MultiCostCenterState (Reset semantics)
│   ├── Presets/                      ScenarioPresets integrity
│   ├── Services/                     AppStateService, UrlStateService (incl. bad-input)
│   └── Components/                   PresetPanel, StatCards (bUnit)
│
└── tests/UBB.E2E/         Playwright E2E tests (URL sharing across all modes)
    └── tests/url-sharing.spec.ts     6 tests: share/restore per mode, URL format, toast
```

---

## Billing flow (as implemented in `RequestFlowEngine.EvaluateStep`)

Each request is evaluated in this order:

1. **User-level budget (ULB)** — most specific limit wins (individual override > universal ULB). Always a hard stop. Block → flow ends.
2. **Shared pool** — if the pool covers the request, consume and allow (no metered charge).
3. **Metered phase** (pool exhausted or partially covering):
   a. **Cost-centre metered budget** — insufficient → block.
   b. **Enterprise metered cap** — insufficient → block.
4. Request allowed with metered billing → `Result = Warn` (paid usage).

Node states (`FlowNode` enum → `FlowNodeState`) drive the diagram colours: Idle (grey), Pass (green), Warn (yellow), Block (red).

**Simulation modes** (`SimulationMode` enum):
- `Single` — one request of configurable credit size
- `Agentic` — multi-step workflow (plan → context → implement → test → review); each step evaluated independently; a block stops the workflow
- `MultiCostCenter` — N cost centers compete for the shared pool round-robin, then fall back to their own metered budgets and the enterprise cap

---

## State management

- **`AppStateService`** (scoped DI) is the single source of truth: `FlowState`, `MultiCCState`, `ActivePresetKey`, `event Action OnChange`.
- All simulation execution routes through it (`RunSingle`, `RunAgentic`, `RunMultiCostCenter`). Components never call `RequestFlowEngine` directly.
- `MultiCCPanel` subscribes to `OnChange` and adopts URL-restored state via `SyncFromAppState()`; every configuration edit publishes back via `SetMultiCCState()`.
- `...WithoutNotifying` loaders exist for URL restore to avoid double-render on startup.

## URL sharing

- `UrlStateService.SerializeAppState()` encodes `{FlowState, MultiCCState?, ActivePresetKey?, Mode}` as JSON → Base64URL (`+`→`-`, `/`→`_`, `=` stripped) into the `#` fragment.
- On first render, `Home.razor` tries the full app-state format, falls back to the legacy flow-state format, and shows a warning toast if the hash is unreadable.
- Share button: updates the address bar (`NavigateTo`, no reload), copies to clipboard, shows a success toast.

---

## Quality gates

| Gate | Tooling | Enforced by |
|------|---------|-------------|
| Build | `-warnaserror`, zero warnings | `qa.yml` + `#qa` agent |
| Unit tests | 145 xUnit/bUnit tests | `qa.yml` + `#qa` agent |
| Coverage | ≥ 80% line on `UBB.Core` (currently ~97%) | `qa.yml` |
| Security | CVE scan, SRI audit, Gitleaks | `security.yml` + `#security` agent |
| E2E | Playwright URL-sharing suite (6 tests) | `e2e-tests.yml` + `run-e2e-tests.ps1/.sh` |

---

## Deployment

Static publish; no server:

```bash
dotnet publish src/UBB/UBB.csproj -c Release -o publish
# deploy publish/wwwroot/** to static hosting (GitHub Pages: adjust <base href> to repo path)
```

`wwwroot/404.html` mirrors `index.html` for SPA routing on GitHub Pages; `.nojekyll` prevents Jekyll from blocking `_framework/`.

> **Gap:** there is no `deploy.yml` workflow yet — publishing is manual (tracked in analysis.md P4).

---

## Known cleanup candidates

- `SimulationConfig` + legacy `Serialize`/`Deserialize`/`PushToUrl` in `UrlStateService` — superseded by `SerializeAppState`; kept for backward-compatible URL decoding.

> `Pages/Counter.razor`, `Pages/Weather.razor`, and the unused `Layout/NavMenu.razor` template leftovers were deleted on 2026-07-09.
