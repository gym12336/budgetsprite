namespace BudgetSprite.Api.Models;

public class Category
{
    public int Id { get; set; }
    public int? UserId { get; set; }           // null = 系统预设
    public string Name { get; set; } = string.Empty;
    public int? ParentId { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public RecordType Type { get; set; }       // 支出或收入
    public int SortOrder { get; set; }

    public User? User { get; set; }
    public Category? Parent { get; set; }
    public ICollection<Category> Children { get; set; } = [];
    public ICollection<Record> Records { get; set; } = [];
}
