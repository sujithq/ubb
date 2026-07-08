namespace UBB.Models;

public class FlowResult
{
    public bool Blocked { get; set; }
    public List<string> Logs { get; set; } = [];
    public Dictionary<FlowNode, FlowNodeState> NodeStates { get; set; } = DefaultNodeStates();
    public decimal UserUsedCredits { get; set; }
    public decimal PoolRemainingCredits { get; set; }
    public decimal CostCenterMeteredRemainingCredits { get; set; }
    public decimal EnterpriseMeteredRemainingCredits { get; set; }

    public static Dictionary<FlowNode, FlowNodeState> DefaultNodeStates() => new()
    {
        [FlowNode.User] = FlowNodeState.Idle,
        [FlowNode.Pool] = FlowNodeState.Idle,
        [FlowNode.Paid] = FlowNodeState.Idle,
        [FlowNode.CostCentre] = FlowNodeState.Idle,
        [FlowNode.Enterprise] = FlowNodeState.Idle,
        [FlowNode.Result] = FlowNodeState.Idle,
    };
}
