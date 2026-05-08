namespace BudgetSprite.Api.Models;

public class Record
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public int AccountId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public RecordType Type { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Note { get; set; }
    public string? Tags { get; set; }           // 逗号分隔
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }

    public User User { get; set; } = null!;
    public FinAccount Account { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public ICollection<RecordImage> Images { get; set; } = [];
}

public enum RecordType : byte
{
    Expense = 0,
    Income = 1,
    Transfer = 2
}
