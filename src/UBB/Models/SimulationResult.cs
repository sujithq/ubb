namespace UBB.Models;

public class SimulationResult
{
    public List<DailySnapshot> Snapshots { get; set; } = [];
    public decimal TotalFixedCost { get; set; }
    public decimal TotalMeteredCost { get; set; }
    public decimal GrandTotal => TotalFixedCost + TotalMeteredCost;
    public int? PoolExhaustedOnDay { get; set; }
    public decimal TotalPoolCredits { get; set; }
}
