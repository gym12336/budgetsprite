namespace BudgetSprite.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<FinAccount> FinAccounts { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Record> Records { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<RecurringRule> RecurringRules { get; set; } = [];
}
