# Plan: GitHub Copilot Spend Simulator — Blazor WASM on GitHub Pages
## (Billing model verified against GitHub docs, July 2026)

## TL;DR
Single-page Blazor WebAssembly (.NET 10) app. Bootstrap 5 from CDN, Blazor.ChartJs for burndown/spend charts. Entire 30-day billing simulation runs in C# in the browser. Config serialized to URL fragment for sharing. Static output deployed to GitHub Pages.

---

## Phase 1 — Project Scaffold

1. Create `UBB.csproj` — Blazor WASM standalone, .NET 10
2. Add NuGet: `Blazor.ChartJs` only; Bootstrap 5 + Chart.js via CDN in `index.html`
3. Configure `wwwroot/index.html` with `<base href="/UBB/">` (adjust to repo name)
4. Add `wwwroot/404.html` (identical to `index.html`) for GitHub Pages SPA routing
5. Add `wwwroot/.nojekyll` to prevent Jekyll blocking `_framework/` files

---

## Phase 2 — Data Models (verified against GitHub docs)

- `LicenseType.cs` — enum: `Business`, `Enterprise`

- `UserConfig.cs` — `Id`, `Name`, `LicenseType`, `IsPowerUser`, `IndividualBudgetDollars?`
  (individual ULB — overrides CC ULB and universal ULB; ALWAYS hard stop, no soft option),
  `DailyUsageRateCredits`, `CostCenterId`

- `CostCenterConfig.cs`:
  - `Id`, `Name`, `Users`
  - `CostCenterUserLevelBudgetDollars?` — per-member cap; active pool AND metered phases; always hard stop; overrides universal ULB for CC members
  - `MeteredBudgetDollars?` — caps metered charges ONLY (not pool usage); activates after pool exhausted
  - `StopOnMeteredExhaustion` bool — "Stop usage when budget limit is reached" for this CC metered budget (default false)
  - `IncludedCreditAllocationCap?` — simulator-only override (GitHub auto-sets this from seat count; manual here for what-if modelling); when reached, CC members overflow or are blocked
  - `BlockOnIncludedCap` bool — whether to block CC members when included credit cap is hit
  - `ExcludedFromEnterpriseBudget` bool — if true, CC metered charges don't count against enterprise budget

- `SimulationConfig.cs`:
  - `BusinessSeats`, `EnterpriseSeats`
  - `UsePromotionalCredits` bool — Business 3,000 / Enterprise 7,000 credits/seat (active June–Sept 2026)
  - `AllowMeteredUsage` bool — mirrors "AI credit paid usage" policy; if false, all usage blocked when pool exhausted regardless of budgets
  - `UniversalUserLevelBudgetDollars?` — default ULB for every licensed user; most specific ULB wins (individual > CC ULB > universal); always hard stop
  - `EnterpriseMeteredBudgetDollars?` — caps metered charges ONLY (not total bill; total = license fees + metered charges)
  - `StopOnEnterpriseMeteredExhaustion` bool — "Stop usage when budget limit is reached" for enterprise budget (default false per docs)
  - `List<CostCenterConfig> CostCenters`

- `DailySnapshot.cs` — `Day` (1–30), `RemainingPoolCredits`, `CumulativeMeteredSpendDollars`, `CostCenterSnapshots` (dict by Guid), `BlockedUsers` (list of Guid), `BlockedCostCenters` (list of Guid)

- `CostCenterDailySnapshot.cs` — `CreditsDrawnFromPool`, `CumulativeMeteredSpend`, `IsBlockedByMeteredBudget`, `IsBlockedByIncludedCap`

- `SimulationResult.cs` — `Snapshots`, `TotalFixedCost`, `TotalMeteredCost`, `GrandTotal`

- `ScenarioPreset.cs` — `Name`, `Description`, factory returning `SimulationConfig`

---

## Phase 3 — Services

### BillingConstants.cs
- `CreditValueDollars = 0.01m`
- `BusinessStandardCredits = 1900`, `BusinessPromoCredits = 3000`
- `EnterpriseStandardCredits = 3900`, `EnterprisePromoCredits = 7000`
- `BusinessSeatCost = 19m`, `EnterpriseSeatCost = 39m`

### BillingEngine.cs — verified billing flow order (from docs):
Each request per user per day:
1. **ULB check** — find most specific ULB (individual > CC ULB > universal). If user is at/over limit → block (always hard stop)
2. **Pool check** — if `AllowMeteredUsage=false` and pool empty → block. If pool has credits → draw from pool (respecting `IncludedCreditAllocationCap` if set)
3. **Metered phase** (pool empty, `AllowMeteredUsage=true`):
   - If user is in a CC with `MeteredBudgetDollars` set → check CC metered budget; if exhausted + `StopOnMeteredExhaustion` → block
   - If CC is `ExcludedFromEnterpriseBudget=false` → also check enterprise metered budget; if exhausted + `StopOnEnterpriseMeteredExhaustion` → block
4. **"Lowest remaining headroom wins"** — whichever budget has least capacity blocks first, regardless of other budgets still having capacity

### AppStateService.cs
Scoped service, single source of truth: `SimulationConfig`, `SimulationResult?`, `event Action OnChange`. CRUD for cost centers/users, re-runs simulation on every change.

### UrlStateService.cs
JSON → Base64Url of `SimulationConfig` into URL `#` fragment. `PushToUrl` / `LoadFromUrl` via `NavigationManager`.

### ScenarioPresets.cs — 3 presets:
1. **Conservative** — 10 Business + 5 Enterprise, low usage (~50 credits/day/user), tight universal ULB ($2), stop on enterprise metered exhaustion
2. **Balanced** — 50 Business + 20 Enterprise, medium usage (~200 credits/day/user), CC user-level budgets ($10), enterprise metered budget ($500)
3. **Heavy AI Usage** — 200 Business + 50 Enterprise, high usage (~500 credits/day/user), promotional credits on, enterprise metered budget with soft stop

---

## Phase 4 — Component Hierarchy

```
MainLayout.razor
└── Index.razor
    ├── ScenarioPresetBar             ← 3 preset buttons + "Share URL" button
    ├── [Left Column]
    │   ├── GlobalSettingsPanel       ← seats, promo toggle, AllowMeteredUsage toggle,
    │   │                                universal ULB, enterprise metered budget + stop toggle
    │   └── CostCenterList
    │       └── CostCenterCard × N
    │           └── CostCenterEditor (Bootstrap offcanvas)
    │               ├── CC metered budget + StopOnMeteredExhaustion toggle
    │               ├── CC user-level budget (per-member)
    │               ├── Included credit cap + BlockOnIncludedCap
    │               ├── ExcludedFromEnterpriseBudget toggle
    │               └── UserList → UserEditor
    └── [Right Column]
        └── SimulationPanel
            ├── KpiCards              ← Fixed cost, metered cost, Grand Total,
            │                            day pool exhausted, # blocked users, # blocked CCs
            ├── CreditBurndownChart   ← line chart, days 1–30, remaining pool credits
            ├── MeteredSpendChart     ← stacked bar, daily metered per CC + enterprise budget line
            ├── CostCenterSummaryChart ← horizontal bar, utilization vs budget per CC
            └── BlockedEntitiesTimeline ← table: when each user/CC became blocked + reason
```

**Shared**: `SliderInput.razor` (range + number synced), `BudgetBadge.razor`, `ConfirmModal.razor`

---

## Phase 5 — Index.razor

- `OnInitializedAsync` → `UrlStateService.LoadFromUrl` → restore state from fragment
- Subscribe to `AppStateService.OnChange` → `StateHasChanged()`
- "Share URL" → update fragment + JS interop `navigator.clipboard.writeText`

---

## Phase 6 — GitHub Pages Deployment

```bash
dotnet publish -c Release -o publish
# deploy publish/wwwroot/** to gh-pages branch
```

`<base href>` must match actual GitHub repo path (e.g., `/UBB/`).

---

## Relevant Files (to create)

- `UBB.csproj`, `Program.cs`, `App.razor`, `Routes.razor`, `_Imports.razor`
- `wwwroot/index.html`, `wwwroot/404.html`, `wwwroot/.nojekyll`, `wwwroot/css/app.css`
- `Models/` — 8 files above
- `Services/BillingConstants.cs`, `BillingEngine.cs`, `AppStateService.cs`, `UrlStateService.cs`, `ScenarioPresets.cs`
- `Components/Layout/MainLayout.razor`
- `Components/Shared/SliderInput.razor`, `BudgetBadge.razor`, `ConfirmModal.razor`
- `Components/Presets/ScenarioPresetBar.razor`
- `Components/Config/GlobalSettingsPanel.razor`
- `Components/CostCenters/CostCenterList.razor`, `CostCenterCard.razor`, `CostCenterEditor.razor`
- `Components/Users/UserList.razor`, `UserEditor.razor`
- `Components/Charts/CreditBurndownChart.razor`, `MeteredSpendChart.razor`, `CostCenterSummaryChart.razor`, `BlockedEntitiesTimeline.razor`
- `Components/Simulation/SimulationPanel.razor`, `KpiCards.razor`, `SimulationDayTable.razor`
- `Pages/Index.razor`

---

## Verification

1. `dotnet build` — zero errors
2. `dotnet run` — default Balanced preset loads at localhost
3. Edit seats → charts update reactively
4. Set CC metered budget to $5 + `StopOnMeteredExhaustion=true` → CC blocked mid-month
5. Set `AllowMeteredUsage=false` → no metered charges appear, users blocked when pool exhausted
6. Individual ULB set lower than CC ULB → individual limit wins (blocks earlier)
7. CC `ExcludedFromEnterpriseBudget=true` → CC metered spend not counted in enterprise budget total
8. Click *Conservative* preset → config + charts reset
9. "Share URL" → paste in new tab → identical config restored
10. `dotnet publish -c Release` → `publish/wwwroot/` contains `index.html`, `404.html`, `.nojekyll`, `_framework/`

---

## Decisions & Scope

- Bootstrap 5 + Chart.js via CDN — no npm/webpack pipeline
- URL state in fragment `#` (never sent to server)
- `System.Text.Json` for serialization (built-in)
- Organization-level budgets NOT modelled (enterprise-only simulator; org budgets are for org-only accounts)
- `IncludedCreditAllocationCap` is a simulator-only override; labelled in UI as "Simulated included credit cap (auto-set by GitHub in production)"
- Promotional credits labelled as "Promotional period (June–Sept 2026)" — matches current docs expiry
- Enterprise metered budget UI note: "This caps metered charges only. Total max bill = license fees + this amount."
- "Stop usage when budget limit is reached" defaults to OFF for CC and enterprise budgets (matching GitHub's actual default)
- No GitHub Actions; manual deploy to `gh-pages` branch
