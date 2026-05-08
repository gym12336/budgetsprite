namespace BudgetSprite.Api.Models;

public class FinAccount
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public decimal Balance { get; set; }
    public string? Note { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Record> Records { get; set; } = [];
}

public enum AccountType : byte
{
    Cash = 0,
    DebitCard = 1,
    CreditCard = 2,
    Alipay = 3,
    Wechat = 4,
    Other = 5
}
