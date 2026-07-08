using FluentAssertions;
using UBB.Models;
using UBB.Services;

namespace UBB.Tests.Engine;

/// <summary>
/// Unit tests for RequestFlowEngine.RunAgentic — 5-step workflow evaluation.
/// Each step is evaluated independently; a block at any step stops the workflow.
/// </summary>
public class AgenticFlowEngineTests
{
    private static RequestFlowState DefaultState(
        decimal userLimit     = 10_000,
        decimal poolRemaining = 390_000,
        decimal ccRemaining   = 200_000,
        decimal entRemaining  = 1_000_000) => new()
    {
        UserType                          = UserType.Standard,
        UniversalLimitCredits             = userLimit,
        PoolRemainingCredits              = poolRemaining,
        CostCenterMeteredRemainingCredits = ccRemaining,
        EnterpriseMeteredRemainingCredits = entRemaining,
        UserUsedCredits                   = 0,
    };

    // Default step total = 700+950+1600+1250+650 = 5150 credits

    [Fact]
    public void AllStepsPass_WhenPoolCoversAll()
    {
        var state = DefaultState(poolRemaining: 390_000);
        var (logs, nodeStates, userUsed, poolRemaining, _, _) = RequestFlowEngine.RunAgentic(state);

        logs.Should().NotContain(l => l.Contains("Stopped at step"));
        nodeStates[FlowNode.Result].Should().Be(FlowNodeState.Pass);
        userUsed.Should().Be(5_150);
        poolRemaining.Should().Be(390_000 - 5_150);
    }

    [Fact]
    public void WorkflowStops_WhenULBExceededMidFlow()
    {
        // ULB = 3000; steps sum = 5150. Should block after plan+inspect+implement = 3250 > 3000
        var state = DefaultState(userLimit: 3_000, poolRemaining: 390_000);
        var (logs, nodeStates, _, _, _, _) = RequestFlowEngine.RunAgentic(state);

        logs.Should().Contain(l => l.Contains("Stopped at step"));
        nodeStates[FlowNode.Result].Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void WorkflowStops_WhenPoolExhaustsAndMeteredBlocks()
    {
        // Pool = 1000, CC budget = 500. After pool, first metered step needs >500 → block
        var state = DefaultState(userLimit: 10_000, poolRemaining: 1_000,
                                  ccRemaining: 500, entRemaining: 1_000_000);
        var (logs, nodeStates, _, _, _, _) = RequestFlowEngine.RunAgentic(state);

        logs.Should().Contain(l => l.Contains("Stopped at step"));
        nodeStates[FlowNode.Result].Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void WorkflowStops_WhenEnterpriseCapExhausts()
    {
        var state = DefaultState(userLimit: 10_000, poolRemaining: 0,
                                  ccRemaining: 200_000, entRemaining: 1_000);
        var (logs, nodeStates, _, _, _, _) = RequestFlowEngine.RunAgentic(state);

        logs.Should().Contain(l => l.Contains("Stopped at step"));
        nodeStates[FlowNode.Result].Should().Be(FlowNodeState.Block);
    }

    [Fact]
    public void AllStepsMetered_WhenPoolEmpty_ButBudgetsSufficient()
    {
        var state = DefaultState(userLimit: 10_000, poolRemaining: 0,
                                  ccRemaining: 200_000, entRemaining: 1_000_000);
        var (logs, nodeStates, userUsed, poolRemaining, ccRemaining, entRemaining)
            = RequestFlowEngine.RunAgentic(state);

        logs.Should().NotContain(l => l.Contains("Stopped at step"));
        nodeStates[FlowNode.Result].Should().Be(FlowNodeState.Warn); // metered pass = Warn
        userUsed.Should().Be(5_150);
        poolRemaining.Should().Be(0);
        ccRemaining.Should().Be(200_000 - 5_150);
        entRemaining.Should().Be(1_000_000 - 5_150);
    }

    [Fact]
    public void Logs_ContainStepNames_ForAllExecutedSteps()
    {
        var state = DefaultState();
        var (logs, _, _, _, _, _) = RequestFlowEngine.RunAgentic(state);

        logs.Should().Contain(l => l.Contains("Plan task"));
        logs.Should().Contain(l => l.Contains("Repository context"));
        logs.Should().Contain(l => l.Contains("Implementation loop"));
    }

    [Fact]
    public void FirstStepBlock_ProducesOneStopEntry()
    {
        // ULB smaller than the first step (700 credits)
        var state = DefaultState(userLimit: 500, poolRemaining: 390_000);
        var (logs, _, _, _, _, _) = RequestFlowEngine.RunAgentic(state);

        logs.Count(l => l.Contains("Stopped at step")).Should().Be(1);
    }
}
