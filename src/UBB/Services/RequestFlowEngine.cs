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
            nodeStates["user"] = FlowNodeState.Block;
            nodeStates["result"] = FlowNodeState.Block;
            logs.Add($"BLOCK {label}: user-level budget exceeded ({userUsed + credits:N0} / {userLimit:N0} credits).");
            return Blocked(logs, nodeStates, userUsed, poolRemaining, costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        nodeStates["user"] = FlowNodeState.Pass;
        logs.Add($"PASS {label}: user-level control passed ({userUsed + credits:N0} / {userLimit:N0} credits).");

        // ── Step 2: Pool check ───────────────────────────────────────────────
        if (poolRemaining >= credits)
        {
            nodeStates["pool"] = FlowNodeState.Pass;
            nodeStates["result"] = FlowNodeState.Pass;
            logs.Add($"PASS {label}: consumed {credits:N0} credits from the shared pool.");
            return Allowed(false, logs, nodeStates,
                userUsed + credits, poolRemaining - credits,
                costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        // ── Step 3: Metered phase ────────────────────────────────────────────
        var poolPart = Math.Max(0, poolRemaining);
        var paidPart = credits - poolPart;

        nodeStates["pool"] = poolPart > 0 ? FlowNodeState.Warn : FlowNodeState.Block;
        nodeStates["paid"] = FlowNodeState.Warn;
        logs.Add($"WARN {label}: pool covers {poolPart:N0}; {paidPart:N0} credits move to metered mode.");

        if (costCenterMeteredRemaining < paidPart)
        {
            nodeStates["costCentre"] = FlowNodeState.Block;
            nodeStates["result"] = FlowNodeState.Block;
            logs.Add($"BLOCK {label}: cost centre requires {paidPart:N0}, remaining {costCenterMeteredRemaining:N0}.");
            return Blocked(logs, nodeStates, userUsed, poolRemaining, costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        nodeStates["costCentre"] = FlowNodeState.Pass;

        if (enterpriseMeteredRemaining < paidPart)
        {
            nodeStates["enterprise"] = FlowNodeState.Block;
            nodeStates["result"] = FlowNodeState.Block;
            logs.Add($"BLOCK {label}: enterprise metered cap requires {paidPart:N0}, remaining {enterpriseMeteredRemaining:N0}.");
            return Blocked(logs, nodeStates, userUsed, poolRemaining, costCenterMeteredRemaining, enterpriseMeteredRemaining);
        }

        nodeStates["enterprise"] = FlowNodeState.Pass;
        nodeStates["result"] = FlowNodeState.Warn;
        logs.Add($"PASS {label}: metered usage allowed for {paidPart:N0} credits (${paidPart * BillingConstants.CreditValueDollars:F2}).");

        return Allowed(true, logs, nodeStates,
            userUsed + credits, 0,
            costCenterMeteredRemaining - paidPart,
            enterpriseMeteredRemaining - paidPart);
    }

    public static (List<string> Logs, Dictionary<string, FlowNodeState> NodeStates,
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

    // ── helpers ──────────────────────────────────────────────────────────────
    private static FlowResult Blocked(List<string> logs, Dictionary<string, FlowNodeState> nodeStates,
        decimal userUsed, decimal poolRemaining, decimal ccRemaining, decimal entRemaining) =>
        new() { Blocked = true, Logs = logs, NodeStates = nodeStates,
                UserUsedCredits = userUsed, PoolRemainingCredits = poolRemaining,
                CostCenterMeteredRemainingCredits = ccRemaining, EnterpriseMeteredRemainingCredits = entRemaining };

    private static FlowResult Allowed(bool metered, List<string> logs, Dictionary<string, FlowNodeState> nodeStates,
        decimal userUsed, decimal poolRemaining, decimal ccRemaining, decimal entRemaining) =>
        new() { Blocked = false, Logs = logs, NodeStates = nodeStates,
                UserUsedCredits = userUsed, PoolRemainingCredits = poolRemaining,
                CostCenterMeteredRemainingCredits = ccRemaining, EnterpriseMeteredRemainingCredits = entRemaining };
}
