namespace BudgetSprite.Api.Models;

public class Budget
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int? CategoryId { get; set; }        // null = 总预算
    public string YearMonth { get; set; } = string.Empty; // "2026-05"
    public decimal Amount { get; set; }

    public User User { get; set; } = null!;
    public Category? Category { get; set; }
}
