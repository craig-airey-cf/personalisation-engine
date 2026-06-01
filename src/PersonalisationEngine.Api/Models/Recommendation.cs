namespace PersonalisationEngine.Api.Models;

public class Recommendation
{
    public int Id { get; set; }
    public string PlayerId { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public bool SafeToShow { get; set; }
    public string? BlockReason { get; set; }
    public string? RecommendationType { get; set; }
    public string? Headline { get; set; }
    public string? Message { get; set; }
    public string? Reason { get; set; }
    public string? SafeOptionsJson { get; set; }
    public DateTime CreatedAt { get; set; }
}
