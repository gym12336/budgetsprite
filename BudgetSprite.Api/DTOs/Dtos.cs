namespace BudgetSprite.Api.DTOs;

// ---- Auth ----
public record RegisterRequest(string Username, string Password, string? Email);
public record LoginRequest(string Username, string Password);
public record LoginResponse(string AccessToken, string RefreshToken, UserDto User);
public record RefreshRequest(string RefreshToken);

// ---- User ----
public record UserDto(int Id, string Username, string? Nickname, string? AvatarUrl);

// ---- Record ----
public record RecordCreateRequest(
    int AccountId,
    int CategoryId,
    decimal Amount,
    byte Type,           // 0支出 1收入 2转账
    DateTime OccurredAt,
    string? Note,
    string? Tags
);

public record RecordUpdateRequest(
    int AccountId,
    int CategoryId,
    decimal Amount,
    byte Type,
    DateTime OccurredAt,
    string? Note,
    string? Tags
);

public record RecordDto(
    long Id,
    int AccountId,
    string AccountName,
    int CategoryId,
    string CategoryName,
    string? CategoryIcon,
    string? CategoryColor,
    decimal Amount,
    byte Type,
    DateTime OccurredAt,
    string? Note,
    string? Tags,
    List<string> ImageUrls
);

public record RecordQueryRequest(
    int Page = 1,
    int PageSize = 20,
    byte? Type = null,
    int? CategoryId = null,
    int? AccountId = null,
    string? Keyword = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
);

public record PagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

// ---- Category ----
public record CategoryCreateRequest(string Name, int? ParentId, string? Icon, string? Color, byte Type);
public record CategoryDto(int Id, string Name, int? ParentId, string? Icon, string? Color, byte Type, int SortOrder, List<CategoryDto> Children);

// ---- FinAccount ----
public record AccountCreateRequest(string Name, byte Type, decimal Balance, string? Note);
public record AccountDto(int Id, string Name, byte Type, decimal Balance, string? Note);
public record TransferRequest(int FromAccountId, int ToAccountId, decimal Amount, string? Note, DateTime OccurredAt);

// ---- Budget ----
public record BudgetUpsertRequest(int? CategoryId, string YearMonth, decimal Amount);
public record BudgetDto(int Id, int? CategoryId, string? CategoryName, string YearMonth, decimal Amount, decimal Used, decimal Remaining);

// ---- Stats ----
public record MonthStatsResponse(
    decimal TotalIncome,
    decimal TotalExpense,
    decimal Balance,
    decimal LastMonthExpense,
    List<CategoryStatItem> CategoryStats,
    List<DailyStatItem> DailyStats
);

public record CategoryStatItem(int CategoryId, string CategoryName, string? Color, decimal Amount, double Percent);
public record DailyStatItem(string Date, decimal Expense, decimal Income);

public record YearStatsResponse(
    List<MonthlyStatItem> MonthlyStats,
    List<CategoryStatItem> TopCategories
);

public record MonthlyStatItem(string YearMonth, decimal Income, decimal Expense);

// ---- User profile ----
public record UpdateProfileRequest(string? Nickname, string? AvatarUrl);
public record ChangePasswordRequest(string OldPassword, string NewPassword);

// ---- Recurring ----
public record RecurringRuleDto(int Id, int CategoryId, string CategoryName, int AccountId, string AccountName, decimal Amount, string? Note, int DayOfMonth, bool IsActive);
public record RecurringRuleRequest(int CategoryId, int AccountId, decimal Amount, string? Note, int DayOfMonth);

// ---- AI Analysis ----
public record AiAnalysisResponse(string Summary, List<AiInsight> Insights);
public record AiInsight(string Type, string Icon, string Title, string Content, string Level);

// ---- Dashboard ----
public record DashboardResponse(
    decimal MonthIncome,
    decimal MonthExpense,
    decimal MonthBalance,
    decimal LastMonthExpense,
    List<AccountDto> Accounts,
    List<BudgetDto> Budgets,
    List<RecordDto> RecentRecords
);
