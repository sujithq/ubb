using UBB.Models;

namespace UBB.Services;

public class AppStateService
{
    public RequestFlowState FlowState { get; private set; } = new();

    /// <summary>Multi-cost-center simulation state, null if in single-user mode.</summary>
    public MultiCostCenterState? MultiCCState { get; private set; }

    /// <summary>Key of the last applied preset, null if manually edited since.</summary>
    public string? ActivePresetKey { get; private set; }

    public event Action? OnChange;

    public void Notify()
    {
        Console.WriteLine($"[AppState.Notify] Triggering OnChange for {OnChange?.GetInvocationList().Length ?? 0} subscribers");
        OnChange?.Invoke();
    }

    public void Reset()
    {
        FlowState = new RequestFlowState();
        ActivePresetKey = null;
        Notify();
    }

    public void LoadFlowState(RequestFlowState state)
    {
        if (state != null)
        {
            FlowState = state;
            ActivePresetKey = null;
            Notify();
        }
    }

    public void LoadFlowStateWithoutNotifying(RequestFlowState state)
    {
        if (state != null)
        {
            FlowState = state;
            ActivePresetKey = null;
            // Don't notify - used during initial load to avoid double render
        }
    }

    public void UpdateField(Action<RequestFlowState> updater)
    {
        updater?.Invoke(FlowState);
        ClearActivePreset();
        Notify();
    }

    public void ClearActivePreset() => ActivePresetKey = null;

    public void SetUserType(UserType type)
    {
        FlowState.UserType = type;
        ActivePresetKey = null; // user type change invalidates the active preset
        Notify();
    }

    public void SetMode(SimulationMode mode)
    {
        FlowState.Mode = mode;
        ActivePresetKey = null; // mode change invalidates the active preset
        Notify();
    }

    public void ApplyPreset(RequestPreset preset)
    {
        ActivePresetKey = preset.Key;
        FlowState.UserType = preset.UserType;
        FlowState.SingleRequestCredits = preset.SingleRequestCredits;
        FlowState.PoolRemainingCredits = preset.PoolRemainingCredits;
        FlowState.UserUsedCredits = preset.UserUsedCredits;
        FlowState.CostCenterMeteredRemainingCredits = preset.CostCenterMeteredRemainingCredits;
        FlowState.EnterpriseMeteredRemainingCredits = preset.EnterpriseMeteredRemainingCredits;
        FlowState.Logs = [$"Preset loaded: {preset.Label}", preset.Description];
        FlowState.NodeStates = FlowResult.DefaultNodeStates();
        Notify();
    }

    public void RunSingle()
    {
        Console.WriteLine($"[AppState.RunSingle] FlowState hash: {FlowState.GetHashCode()}, UserUsedCredits: {FlowState.UserUsedCredits}");
        var result = RequestFlowEngine.EvaluateStep(
            "Single request",
            FlowState.SingleRequestCredits,
            FlowState.UserUsedCredits,
            FlowState.ActiveUserLimit,
            FlowState.PoolRemainingCredits,
            FlowState.CostCenterMeteredRemainingCredits,
            FlowState.EnterpriseMeteredRemainingCredits);

        ApplyResult(result);
        Console.WriteLine($"[AppState.RunSingle] After apply - UserUsedCredits: {FlowState.UserUsedCredits}, calling Notify()");
        Notify();
    }

    public void RunAgentic()
    {
        var (logs, nodeStates, userUsed, poolRemaining, ccRemaining, entRemaining) =
            RequestFlowEngine.RunAgentic(FlowState);

        FlowState.UserUsedCredits = userUsed;
        FlowState.PoolRemainingCredits = poolRemaining;
        FlowState.CostCenterMeteredRemainingCredits = ccRemaining;
        FlowState.EnterpriseMeteredRemainingCredits = entRemaining;
        FlowState.Logs = logs;
        FlowState.NodeStates = nodeStates;
        Notify();
    }

    public void RunMultiCostCenter(MultiCostCenterState state)
    {
        if (state == null) return;
        MultiCCState = state;
        // Don't clear logs — let them accumulate across runs so users see the progression
        RequestFlowEngine.RunMultiCostCenter(state);
        Notify();
    }

    public void UpdateStepCredits(string stepId, decimal credits)
    {
        var step = FlowState.Steps.FirstOrDefault(s => s.Id == stepId);
        if (step is not null)
        {
            step.Credits = Math.Max(0, credits);
            Notify();
        }
    }

    private void ApplyResult(FlowResult result)
    {
        FlowState.UserUsedCredits = result.UserUsedCredits;
        FlowState.PoolRemainingCredits = result.PoolRemainingCredits;
        FlowState.CostCenterMeteredRemainingCredits = result.CostCenterMeteredRemainingCredits;
        FlowState.EnterpriseMeteredRemainingCredits = result.EnterpriseMeteredRemainingCredits;
        FlowState.Logs = result.Logs;
        FlowState.NodeStates = result.NodeStates;
    }
}
