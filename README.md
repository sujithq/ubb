# GitHub Copilot UBB Simulator

A static [Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/hosting-models#blazor-webassembly) application that simulates GitHub Copilot usage-based billing (UBB) governance — showing exactly how AI credit requests flow through each budget guardrail in real time.

## What It Does

Configure billing guardrails and observe how a single request — or a multi-step agentic coding workflow — is evaluated at each checkpoint:

| Step | Guardrail | Active when |
|------|-----------|-------------|
| 1 | **User-level budget (ULB)** | Always — hard stop, no soft option |
| 2 | **Shared pool** | Always — included credits drawn first |
| 3 | **Paid mode** | Pool exhausted + paid usage policy enabled |
| 4 | **Cost centre budget** | Metered phase only |
| 5 | **Enterprise cap** | Metered phase only — final guardrail |

## Billing Model (verified against GitHub docs)

| Concept | Value |
|---------|-------|
| 1 AI credit | $0.01 USD |
| Copilot Business | $19/seat · 1,900 included credits/seat/month |
| Copilot Enterprise | $39/seat · 3,900 included credits/seat/month |
| Promotional period (Jun–Sep 2026) | Business 3,000 · Enterprise 7,000 credits/seat |
| Code completions / next-edit suggestions | **Not billed** — unlimited for all paid plans |

> **Total monthly bill** = License fees + Metered charges (after included credits exhausted)
>
> The enterprise metered budget caps **metered charges only** — it is not a total spend cap.

## Features

- **Request flow simulator** — single request or 5-step agentic workflow (plan → context → implement → test → review)
- **Multi-cost-center mode** — N cost centers competing for shared pool and enterprise cap, with per-CC flow diagrams
- **Live flow diagram** — 6 nodes colour-coded pass / warn / block in real time
- **Execution log** — decision trail showing exactly which guardrail fired and why
- **15 scenario presets** — 6 single-user + 9 multi-cost-center scenarios
- **URL sharing** — Share button encodes the full simulation state (mode, preset, budgets, cost centers) into a copyable link
- **Hover tooltips** — every label and flow node links to the relevant GitHub docs section
- **Standard user vs Architect override** — switches between ULB and individual budget mode

## Preset Scenarios

Click any scenario button in the simulator to auto-populate all controls and observe the billing outcome:

### Single-User Scenarios (Checkpoints 1–5)

| # | Scenario | User Type | Request Credits | Pool Remaining | Cost Centre | Enterprise | Expected Result |
|----|----------|-----------|-----------------|----------------|-------------|-----------|---|
| 1 | **Normal dev request** | Standard | 1,200 | 390,000 | 200,000 | 1,000,000 | ✅ **PASS** — drawn from shared pool, no metered charge |
| 2 | **Power user spike** | Standard | 4,000 | 390,000 | 200,000 | 1,000,000 | ❌ **BLOCK** — ULB exceeded (2,500 limit), request stopped immediately |
| 3 | **Architect override** | Architect | 6,000 | 390,000 | 200,000 | 1,000,000 | ✅ **PASS** — individual limit (8,000) allows it, pool covers full amount |
| 4 | **Pool exhaustion** | Architect | 4,000 | 2,000 | 200,000 | 1,000,000 | ⚠️ **WARN→PASS** — 2k from pool, 2k from metered (CC & enterprise budgets sufficient) |
| 5 | **Cost centre block** | Architect | 6,000 | 0 | 3,000 | 1,000,000 | ❌ **BLOCK** — pool empty, cost centre metered budget insufficient for 6,000 credits |
| 6 | **Enterprise hard stop** | Architect | 6,000 | 0 | 200,000 | 3,000 | ❌ **BLOCK** — pool empty, CC has budget, but enterprise cap only has 3,000 left |

### Multi-Cost-Center Scenarios (Organizational Scale)

Switch to **Multi-cost-center** mode to see how multiple teams compete for shared pool and enterprise budget:

| # | Scenario | Cost Centers | Pool | Enterprise Cap | Expected Outcome |
|---|----------|------|------|---|---|
| 7 | **Multi-CC Normal** | Engineering (10u, 200k), Research (5u, 150k), Sales (3u, 100k) | 390,000 | 1,000,000 | ✅ **ALL PASS** — shared pool sufficient for all three CCs to make 2,000-credit requests |
| 8 | **Multi-CC Pool Exhaustion** | Engineering, Research, Sales (same budgets) | 4,000 | 1,000,000 | ⚠️ **WARN→PASS** — pool covers first ~2 requests, 3rd CC enters metered phase; all CCs have enough metered budget |
| 9 | **Multi-CC Enterprise Block** | Engineering, Research, Sales (same budgets) | 0 | 4,000 | ❌ **PARTIAL BLOCK** — pool empty, CCs have metered budgets, but enterprise cap only supports ~2 requests before exhaustion |
| 10 | **Unequal Budgets** | Engineering (10u, 250k), Research (5u, 150k), Support (2u, 8k) | 2,000 | 500,000 | ⚠️ **MIXED** — Engineering passes (pool), Research enters metered (OK), Support **BLOCKED** (metered budget exhausted) |
| 11 | **One CC Bottleneck** | Engineering (10u, 300k), Research (5u, 50), Sales (3u, 150k) | 100,000 | 800,000 | ⚠️ **MIXED** — Engineering passes, Research **BLOCKED** (metered exhausted), Sales can proceed |
| 12 | **Tight Enterprise Cap** | Engineering (10u, 200k), Research (5u, 200k), Sales (3u, 200k) | 50,000 | 2,000 | ⚠️ **2/3 PASS** — All CCs attempt metered; 1st & 2nd pass, 3rd **BLOCKED** by enterprise cap |
| 13 | **Sequential Metered** | Engineering (10u, 8k), Research (5u, 8k), Sales (3u, 8k) | 1,500 | 10,000 | ⚠️ **PARTIAL** — Pool exhausted early, all CCs in metered; Enterprise cap blocks 3rd CC |
| 14 | **Large Spike** | Engineering (10u, 300k), Research (5u, 100k), Sales (3u, 100k) | 2,500 | 6,000 | ⚠️ **CRITICAL** — Large Engineering request (3k) exhausts pool; Research/Sales compete in metered with tight enterprise cap |
| 15 | **Edge Case** | Engineering (10u, 300k), Research (5u, 300k), Sales (3u, 300k) | 2,000 | 2,100 | ❌ **2 BLOCK** — Pool exactly covers 1st CC; 2nd enters metered (OK); 3rd **BLOCKED** by enterprise cap |

## Tech Stack

- [.NET 10 Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — no backend, all calculations in-browser via C#
- [Bootstrap 5.3.8](https://getbootstrap.com/) — vendored locally in `wwwroot/lib/bootstrap/` (no CDN dependency at runtime)
- `System.Text.Json` — URL state serialization (built into .NET)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run locally

```bash
cd src/UBB
dotnet run
```

Open http://localhost:5295 in your browser.

### Hot reload

```bash
cd src/UBB
dotnet watch
```

## Testing

```bash
# Unit tests (xUnit + FluentAssertions + bUnit — 145 tests)
dotnet test tests/UBB.Tests/

# E2E tests (Playwright — URL sharing across all simulation modes)
./run-e2e-tests.ps1        # Windows
./run-e2e-tests.sh         # macOS / Linux
```

CI runs three workflows on every push and PR: **QA** (build + tests + coverage ≥ 80%), **Security** (CVE scan, SRI audit, secret scan), and **E2E** (Playwright).

## Deploy to GitHub Pages

1. Publish the release build:

   ```bash
   dotnet publish src/UBB -c Release -o publish
   ```

2. Copy `publish/wwwroot/` contents to the `gh-pages` branch.

3. **Update `<base href>`** in both `wwwroot/index.html` and `wwwroot/404.html` to match your repository path:
   - User/org pages site (`username.github.io`): keep `<base href="/" />`
   - Project site (`username.github.io/repo-name`): change to `<base href="/repo-name/" />`

The `wwwroot/404.html` (identical to `index.html`) handles direct-link refreshes. The `wwwroot/.nojekyll` file prevents GitHub Pages from ignoring the `_framework/` folder.

## Project Structure

```
.
├── plan.md                   ← As-built architecture doc
├── analysis.md               ← 12-factor / SOLID / tech-debt working document
├── src/
│   ├── UBB.Core/             ← Pure C# domain library (no Blazor dependencies)
│   │   ├── Models/           ← RequestFlowState, MultiCostCenterState, CostCenterBudget,
│   │   │                        FlowResult, FlowNode, RequestPreset, MultiCCPreset, …
│   │   └── Services/
│   │       ├── RequestFlowEngine.cs   ← stateless engine: single, agentic, multi-CC
│   │       ├── BillingConstants.cs    ← credit values, seat costs, promo amounts
│   │       └── ScenarioPresets.cs     ← 15 preset configurations (6 single + 9 multi-CC)
│   └── UBB/                  ← Blazor WASM app (UI only; references UBB.Core)
│       ├── Services/
│       │   ├── AppStateService.cs     ← single source of truth + OnChange event bus
│       │   └── UrlStateService.cs     ← Base64URL encode/decode for URL sharing
│       ├── Components/
│       │   ├── InfoTip.razor          ← ⓘ popover with optional docs link
│       │   ├── StatCards.razor        ← live metric cards
│       │   ├── FlowDiagram.razor      ← 6-node billing flow diagram
│       │   ├── MultiCCFlowDiagram.razor ← per-CC 5-node diagram
│       │   ├── MultiCCPanel.razor     ← multi-CC configuration + simulation
│       │   ├── ExecutionLog.razor     ← colour-coded decision trail
│       │   ├── PresetPanel.razor      ← scenario preset buttons
│       │   └── AgenticWorkflow.razor  ← editable 5-step agentic workflow
│       ├── Pages/Home.razor           ← single-page simulator UI + URL restore
│       └── wwwroot/
│           ├── index.html             ← CSP, vendored Bootstrap 5.3.8, js/ubb.js helpers
│           ├── 404.html               ← SPA fallback for GitHub Pages
│           ├── lib/bootstrap/         ← vendored Bootstrap (css + bundle js)
│           ├── .nojekyll              ← prevents Jekyll processing
│           └── css/app.css            ← UBB-specific styles
├── tests/
│   ├── UBB.Tests/            ← xUnit + FluentAssertions + bUnit (145 tests)
│   └── UBB.E2E/              ← Playwright E2E tests (URL sharing)
├── .github/
│   ├── workflows/            ← qa.yml · security.yml · e2e-tests.yml
│   └── agents/               ← #qa and #security local agents
└── UBB.slnx
```

## GitHub Copilot Billing References

- [What are GitHub AI Credits?](https://docs.github.com/en/copilot/concepts/billing/usage-based-billing-for-organizations-and-enterprises#what-are-github-ai-credits)
- [How AI credits work (pooling, promo credits)](https://docs.github.com/en/copilot/concepts/billing/usage-based-billing-for-organizations-and-enterprises#how-do-ai-credits-work)
- [What is billed in AI credits?](https://docs.github.com/en/copilot/concepts/billing/usage-based-billing-for-organizations-and-enterprises#what-is-billed-in-ai-credits)
- [Budgets for usage-based billing](https://docs.github.com/en/copilot/concepts/billing/budgets-for-usage-based-billing)
- [How billing flows through budgets](https://docs.github.com/en/copilot/concepts/billing/budgets-for-usage-based-billing#how-billing-flows-through-budgets)
- [What happens when a user is blocked](https://docs.github.com/en/copilot/concepts/billing/budgets-for-usage-based-billing#what-happens-when-a-user-is-blocked)
- [About billing for GitHub Copilot in your enterprise](https://docs.github.com/en/copilot/managing-copilot/managing-copilot-for-your-enterprise/managing-the-copilot-plan-for-your-enterprise/about-billing-for-github-copilot-in-your-enterprise)
