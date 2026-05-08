namespace BudgetSprite.Api.Models;

public class RecurringRule
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public int DayOfMonth { get; set; }         // 每月几号自动生成
    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
    public FinAccount Account { get; set; } = null!;
}
