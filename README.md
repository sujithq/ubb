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
- **Live flow diagram** — 6 nodes colour-coded pass / warn / block in real time
- **Execution log** — decision trail showing exactly which guardrail fired and why
- **6 scenario presets** — Normal dev, Power user spike, Architect override, Pool exhaustion, Cost centre block, Enterprise hard stop
- **Hover tooltips** — every label and flow node links to the relevant GitHub docs section
- **Standard user vs Architect override** — switches between ULB and individual budget mode

## Preset Scenarios

Click any scenario button in the simulator to auto-populate all controls and observe the billing outcome:

| # | Scenario | User Type | Request Credits | Pool Remaining | Cost Centre | Enterprise | Expected Result |
|----|----------|-----------|-----------------|----------------|-------------|-----------|---|
| 1 | **Normal dev request** | Standard | 1,200 | 390,000 | 200,000 | 1,000,000 | ✅ **PASS** — drawn from shared pool, no metered charge |
| 2 | **Power user spike** | Standard | 4,000 | 390,000 | 200,000 | 1,000,000 | ❌ **BLOCK** — ULB exceeded (2,500 limit), request stopped immediately |
| 3 | **Architect override** | Architect | 6,000 | 390,000 | 200,000 | 1,000,000 | ✅ **PASS** — individual limit (8,000) allows it, pool covers full amount |
| 4 | **Pool exhaustion** | Architect | 4,000 | 2,000 | 200,000 | 1,000,000 | ⚠️ **WARN→PASS** — 2k from pool, 2k from metered (CC & enterprise budgets sufficient) |
| 5 | **Cost centre block** | Architect | 6,000 | 0 | 3,000 | 1,000,000 | ❌ **BLOCK** — pool empty, cost centre metered budget insufficient for 6,000 credits |
| 6 | **Enterprise hard stop** | Architect | 6,000 | 0 | 200,000 | 3,000 | ❌ **BLOCK** — pool empty, CC has budget, but enterprise cap only has 3,000 left |

## Tech Stack

- [.NET 10 Blazor WebAssembly](https://learn.microsoft.com/en-us/aspnet/core/blazor/) — no backend, all calculations in-browser via C#
- [Bootstrap 5.3.8](https://getbootstrap.com/) — UI via CDN (no npm/build pipeline)
- `System.Text.Json` — URL state serialization (built into .NET)

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Run locally

```bash
cd src/UBB
dotnet run
```

Open http://localhost:5000 in your browser.

### Hot reload

```bash
cd src/UBB
dotnet watch
```

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
├── plan.md                   ← Architecture & billing model decisions
├── src/
│   └── UBB/
│       ├── Models/           ← LicenseType, UserConfig, CostCenterConfig,
│       │                        SimulationConfig, RequestFlowState, …
│       ├── Services/
│       │   ├── BillingConstants.cs    ← credit values, seat costs
│       │   ├── RequestFlowEngine.cs   ← C# port of the JS evaluateStep engine
│       │   ├── AppStateService.cs     ← singleton state + event bus
│       │   ├── UrlStateService.cs     ← Base64Url encode/decode for URL sharing
│       │   └── ScenarioPresets.cs     ← 6 preset configurations
│       ├── Components/
│       │   ├── InfoTip.razor          ← ⓘ popover with optional docs link
│       │   ├── StatCards.razor        ← 4 live metric cards
│       │   ├── FlowDiagram.razor      ← 6-node billing flow diagram
│       │   ├── ExecutionLog.razor     ← colour-coded decision trail
│       │   ├── PresetPanel.razor      ← scenario preset buttons
│       │   └── AgenticWorkflow.razor  ← editable 5-step agentic workflow
│       ├── Pages/Home.razor           ← single-page simulator UI
│       └── wwwroot/
│           ├── index.html             ← Bootstrap 5.3.8 CDN, window.ubb helpers
│           ├── 404.html               ← SPA fallback for GitHub Pages
│           ├── .nojekyll              ← prevents Jekyll processing
│           └── css/app.css            ← UBB-specific styles
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
