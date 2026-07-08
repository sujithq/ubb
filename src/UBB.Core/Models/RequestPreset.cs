namespace UBB.Models;

public class RequestPreset
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    public string Description { get; set; } = "";
    public UserType UserType { get; set; } = UserType.Standard;
    public decimal SingleRequestCredits { get; set; }
    public decimal PoolRemainingCredits { get; set; }
    public decimal UserUsedCredits { get; set; }
    public decimal CostCenterMeteredRemainingCredits { get; set; }
    public decimal EnterpriseMeteredRemainingCredits { get; set; }
}
