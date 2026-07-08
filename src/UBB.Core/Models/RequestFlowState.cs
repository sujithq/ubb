namespace UBB.Models;

public class RequestFlowState
{
    public decimal UniversalLimitCredits { get; set; } = 2_500;
    public decimal IndividualLimitCredits { get; set; } = 8_000;
    public UserType UserType { get; set; } = UserType.Standard;
    public decimal SingleRequestCredits { get; set; } = 2_000;
    public decimal UserUsedCredits { get; set; } = 0;
    public decimal PoolRemainingCredits { get; set; } = 390_000;
    public decimal CostCenterMeteredRemainingCredits { get; set; } = 200_000;
    public decimal EnterpriseMeteredRemainingCredits { get; set; } = 1_000_000;
    public SimulationMode Mode { get; set; } = SimulationMode.Single;
    public List<AgenticStep> Steps { get; set; } = DefaultAgenticSteps();
    public List<string> Logs { get; set; } = [];
    public Dictionary<FlowNode, FlowNodeState> NodeStates { get; set; } = FlowResult.DefaultNodeStates();

    public decimal ActiveUserLimit =>
        UserType == UserType.Architect ? IndividualLimitCredits : UniversalLimitCredits;

    public decimal TotalAgenticCredits =>
        Steps.Sum(s => s.Credits);

    public static List<AgenticStep> DefaultAgenticSteps() =>
    [
        new() { Id = "plan",      Name = "Plan task",             Description = "Agent creates an implementation plan and inspects relevant files.",       Credits = 700 },
        new() { Id = "inspect",   Name = "Repository context",    Description = "Agent loads context, dependencies, tests, and related code paths.",      Credits = 950 },
        new() { Id = "implement", Name = "Implementation loop",   Description = "Agent edits multiple files and iterates on the implementation.",         Credits = 1_600 },
        new() { Id = "test",      Name = "Test and fix loop",     Description = "Agent runs tests, reads failures, and applies corrections.",             Credits = 1_250 },
        new() { Id = "review",    Name = "Review and PR summary", Description = "Agent summarises changes, risks, and follow-up actions.",               Credits = 650 },
    ];
}
