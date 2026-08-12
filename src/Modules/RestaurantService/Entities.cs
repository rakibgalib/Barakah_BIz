namespace Barakah.RestaurantService.Entities;

public class MenuItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public required string Category { get; set; }
    /// <summary>Comma-separated allergen tags, e.g. "nuts,dairy,gluten" — used by the Allergy Management endpoint.</summary>
    public string? AllergenTags { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
