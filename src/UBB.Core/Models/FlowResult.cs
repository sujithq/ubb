namespace UBB.Models;

public class FlowResult
{
    public bool Blocked { get; set; }
    public List<string> Logs { get; set; } = [];
    public Dictionary<string, FlowNodeState> NodeStates { get; set; } = DefaultNodeStates();
    public decimal UserUsedCredits { get; set; }
    public decimal PoolRemainingCredits { get; set; }
    public decimal CostCenterMeteredRemainingCredits { get; set; }
    public decimal EnterpriseMeteredRemainingCredits { get; set; }

    public static Dictionary<string, FlowNodeState> DefaultNodeStates() => new()
    {
        ["user"] = FlowNodeState.Idle,
        ["pool"] = FlowNodeState.Idle,
        ["paid"] = FlowNodeState.Idle,
        ["costCentre"] = FlowNodeState.Idle,
        ["enterprise"] = FlowNodeState.Idle,
        ["result"] = FlowNodeState.Idle,
    };
}
