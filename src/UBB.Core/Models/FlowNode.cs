namespace UBB.Models;

/// <summary>
/// Represents the distinct steps in the billing flow.
/// Replaces magic string keys for compile-time safety and clarity.
/// </summary>
public enum FlowNode
{
    User,
    Pool,
    Paid,
    CostCentre,
    Enterprise,
    Result,
}
