namespace BudgetSprite.Api.Models;

public class RecordImage
{
    public int Id { get; set; }
    public long RecordId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Record Record { get; set; } = null!;
}
