using UBB.Models;

namespace UBB.Services;

/// <summary>
/// Pure stateless engine — faithful C# port of the JS evaluateStep logic.
/// Billing flow order (verified against GitHub docs):
///   1. ULB check   → always hard stop
///   2. Pool check  → draw from shared pool
///   3. Metered     → cost centre budget → enterprise budget
/// </summary>
public static class RequestFlowEngine
{
    public static FlowResult EvaluateStep(
        string label,
        decimal credits,
        decimal userUsed,
        decimal userLimit,
        decimal poolRemaining,
        decimal costCenterMeteredRemaining,
        decimal enterpriseMeteredRemaining)
    {
        var nodeStates = FlowResult.DefaultNodeStates();
        var logs = new List<string>();

        // ── Step 1: ULB check ────────────────────────────────────────────────
        if (userUsed + credits > userLimit)
        {
            nodeStates[FlowNode.User] = FlowNodeState.Block;
            nodeStates[FlowNode.Result] = FlowNodeState.Block;
            logs.Add($"BLOCK {label}: user-level budget exceeded ({userUsed + credits:N0} / {userLimit:N0} credits).");
            return Blocked(logs, nodeStates, userUsed, poolRemaining, costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        nodeStates[FlowNode.User] = FlowNodeState.Pass;
        logs.Add($"PASS {label}: user-level control passed ({userUsed + credits:N0} / {userLimit:N0} credits).");

        // ── Step 2: Pool check ───────────────────────────────────────────────
        if (poolRemaining >= credits)
        {
            nodeStates[FlowNode.Pool] = FlowNodeState.Pass;
            nodeStates[FlowNode.Result] = FlowNodeState.Pass;
            logs.Add($"PASS {label}: consumed {credits:N0} credits from the shared pool.");
            return Allowed(false, logs, nodeStates,
                userUsed + credits, poolRemaining - credits,
                costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        // ── Step 3: Metered phase ────────────────────────────────────────────
        var poolPart = Math.Max(0, poolRemaining);
        var paidPart = credits - poolPart;

        nodeStates[FlowNode.Pool] = poolPart > 0 ? FlowNodeState.Warn : FlowNodeState.Block;
        nodeStates[FlowNode.Paid] = FlowNodeState.Warn;
        logs.Add($"WARN {label}: pool covers {poolPart:N0}; {paidPart:N0} credits move to metered mode.");

        if (costCenterMeteredRemaining < paidPart)
        {
            nodeStates[FlowNode.CostCentre] = FlowNodeState.Block;
            nodeStates[FlowNode.Result] = FlowNodeState.Block;
            logs.Add($"BLOCK {label}: cost centre requires {paidPart:N0}, remaining {costCenterMeteredRemaining:N0}.");
            return Blocked(logs, nodeStates, userUsed, poolRemaining, costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        nodeStates[FlowNode.CostCentre] = FlowNodeState.Pass;

        if (enterpriseMeteredRemaining < paidPart)
        {
            nodeStates[FlowNode.Enterprise] = FlowNodeState.Block;
            nodeStates[FlowNode.Result] = FlowNodeState.Block;
            logs.Add($"BLOCK {label}: enterprise metered cap requires {paidPart:N0}, remaining {enterpriseMeteredRemaining:N0}.");
            return Blocked(logs, nodeStates, userUsed, poolRemaining, costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        nodeStates[FlowNode.Enterprise] = FlowNodeState.Pass;
        nodeStates[FlowNode.Result] = FlowNodeState.Warn;
        logs.Add($"PASS {label}: metered usage allowed for {paidPart:N0} credits (${paidPart * BillingConstants.CreditValueDollars:F2}).");

        return Allowed(true, logs, nodeStates,
            userUsed + credits, 0,
            costCenterMeteredRemaining - paidPart,
            enterpriseMeteredRemaining - paidPart);
    }

    public static (List<string> Logs, Dictionary<FlowNode, FlowNodeState> NodeStates,
                   decimal UserUsed, decimal PoolRemaining, decimal CcRemaining, decimal EntRemaining)
        RunAgentic(RequestFlowState state)
    {
        var allLogs = new List<string>
        {
            $"Agentic workflow started — {state.Steps.Count} steps, {state.TotalAgenticCredits:N0} planned credits."
        };
        var lastNodeStates = FlowResult.DefaultNodeStates();

        var userUsed = state.UserUsedCredits;
        var poolRemaining = state.PoolRemainingCredits;
        var ccRemaining = state.CostCenterMeteredRemainingCredits;
        var entRemaining = state.EnterpriseMeteredRemainingCredits;

        foreach (var step in state.Steps)
        {
            var result = EvaluateStep(step.Name, step.Credits, userUsed,
                state.ActiveUserLimit, poolRemaining, ccRemaining, entRemaining);

            allLogs.AddRange(result.Logs);
            lastNodeStates = result.NodeStates;
            userUsed = result.UserUsedCredits;
            poolRemaining = result.PoolRemainingCredits;
            ccRemaining = result.CostCenterMeteredRemainingCredits;
            entRemaining = result.EnterpriseMeteredRemainingCredits;

            if (result.Blocked)
            {
                allLogs.Add($"Stopped at step: {step.Name}");
                break;
            }
        }

        return (allLogs, lastNodeStates, userUsed, poolRemaining, ccRemaining, entRemaining);
    }

    /// <summary>
    /// Process multiple cost centers making sequential requests.
    /// Each CC has users making requests; pool is shared; metered budgets per CC + enterprise cap.
    /// </summary>
    public static void RunMultiCostCenter(MultiCostCenterState state)
    {
        // Add separator to distinguish runs (don't clear previous logs — accumulate them)
        if (state.Logs.Count > 0)
            state.AddLog("─────────────────────────────────────────────────────────");
        state.AddLog($"[Run #{state.Logs.Count / 10 + 1}] Shared pool: {state.PoolRemainingCredits:N0} | Enterprise cap: {state.EnterpriseMeteredRemainingCredits:N0} | Request size: {state.RequestCreditsPerCC:N0}");
        
        foreach (var cc in state.CostCenters)
        {
            cc.ResetNodeStates();
            state.AddLog($"");
            state.AddLog($"=== {cc.Name} (users: {cc.UserCount}, metered budget: {cc.MeteredRemainingCredits:N0}) ===");

            // Each user in the CC makes a request
            for (int user = 1; user <= cc.UserCount; user++)
            {
                var requestCredits = state.RequestCreditsPerCC;
                
                // Step 1: Can CC draw from pool?
                if (state.PoolRemainingCredits >= requestCredits)
                {
                    state.PoolRemainingCredits -= requestCredits;
                    cc.CreditsConsumed += requestCredits;
                    cc.NodeStates[FlowNode.Pool]   = FlowNodeState.Pass;
                    cc.NodeStates[FlowNode.Result] = FlowNodeState.Pass;
                    state.AddLog($"  User {user}: ✓ drew {requestCredits:N0} from pool (pool now: {state.PoolRemainingCredits:N0})");
                    continue;
                }

                // Step 2: Pool partial + metered
                var poolPart = state.PoolRemainingCredits;
                var meteredNeeded = requestCredits - poolPart;

                if (state.PoolRemainingCredits > 0)
                {
                    cc.NodeStates[FlowNode.Pool] = FlowNodeState.Warn;
                    state.PoolRemainingCredits = 0;
                    state.AddLog($"  User {user}: ✓ drew {poolPart:N0} from pool (pool exhausted)");
                }
                else
                {
                    cc.NodeStates[FlowNode.Pool] = FlowNodeState.Block;
                }

                cc.NodeStates[FlowNode.Paid] = FlowNodeState.Warn;

                // Step 3: Can CC cover metered from its budget?
                var ccPay = Math.Min(meteredNeeded, cc.MeteredRemainingCredits);
                if (ccPay < meteredNeeded)
                {
                    cc.NodeStates[FlowNode.CostCentre] = FlowNodeState.Block;
                    cc.NodeStates[FlowNode.Result]     = FlowNodeState.Block;
                    state.AddLog($"  User {user}: ✗ blocked - needs {meteredNeeded:N0}, CC has {cc.MeteredRemainingCredits:N0}");
                    continue;
                }

                cc.NodeStates[FlowNode.CostCentre] = FlowNodeState.Pass;

                // Step 4: Can enterprise cap cover?
                if (state.EnterpriseMeteredRemainingCredits < meteredNeeded)
                {
                    cc.NodeStates[FlowNode.Enterprise] = FlowNodeState.Block;
                    cc.NodeStates[FlowNode.Result]     = FlowNodeState.Block;
                    state.AddLog($"  User {user}: ✗ blocked - enterprise cap insufficient ({state.EnterpriseMeteredRemainingCredits:N0} / {meteredNeeded:N0})");
                    continue;
                }

                cc.NodeStates[FlowNode.Enterprise] = FlowNodeState.Pass;
                cc.NodeStates[FlowNode.Result]     = FlowNodeState.Warn;
                cc.ConsumeMetered(meteredNeeded);
                state.EnterpriseMeteredRemainingCredits -= meteredNeeded;
                state.AddLog($"  User {user}: ✓ drew {meteredNeeded:N0} from metered (CC: {cc.MeteredRemainingCredits:N0} | Ent: {state.EnterpriseMeteredRemainingCredits:N0})");
            }
        }

        state.AddLog($"");
        state.AddLog("=== SUMMARY ===");
        foreach (var cc in state.CostCenters)
        {
            state.AddLog($"{cc.Name}: consumed {cc.CreditsConsumed:N0} | metered remaining {cc.MeteredRemainingCredits:N0}");
        }
        state.AddLog($"Shared pool: {state.PoolRemainingCredits:N0}");
        state.AddLog($"Enterprise metered: {state.EnterpriseMeteredRemainingCredits:N0}");
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private static FlowResult Blocked(List<string> logs, Dictionary<FlowNode, FlowNodeState> nodeStates,
        decimal userUsed, decimal poolRemaining, decimal ccRemaining, decimal entRemaining) =>
        new() { Blocked = true, Logs = logs, NodeStates = nodeStates,
                UserUsedCredits = userUsed, PoolRemainingCredits = poolRemaining,
                CostCenterMeteredRemainingCredits = ccRemaining, EnterpriseMeteredRemainingCredits = entRemaining };

    private static FlowResult Allowed(bool metered, List<string> logs, Dictionary<FlowNode, FlowNodeState> nodeStates,
        decimal userUsed, decimal poolRemaining, decimal ccRemaining, decimal entRemaining) =>
        new() { Blocked = false, Logs = logs, NodeStates = nodeStates,
                UserUsedCredits = userUsed, PoolRemainingCredits = poolRemaining,
                CostCenterMeteredRemainingCredits = ccRemaining, EnterpriseMeteredRemainingCredits = entRemaining };
}
