namespace Barakah.ClothingService.Entities;

/// <summary>
/// Deliberately the only entity in this service. doc.html lists six Clothing Extension features;
/// five (Size Recommendation, Trend Prediction, Color Coordination, Virtual Try-On, Outfit
/// Suggestions) need real prediction/vision models and are deferred to the AI Integration phase.
/// Sustainability is the one that's plain data, so it's the one built.
/// </summary>
public class SustainabilityRating
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public int Score { get; set; }
    /// <summary>Comma-separated, e.g. "organic,recycled,fair-trade".</summary>
    public string? Certifications { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
