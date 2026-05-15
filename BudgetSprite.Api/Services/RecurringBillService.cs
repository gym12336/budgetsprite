using BudgetSprite.Api.Data;
using BudgetSprite.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Services;

public class RecurringBillService(IServiceScopeFactory scopeFactory, ILogger<RecurringBillService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // 计算距下一个凌晨 1 点的等待时间
            var now = DateTime.Now;
            var next = now.Date.AddDays(1).AddHours(1);
            var delay = next - now;
            await Task.Delay(delay, stoppingToken);

            if (!stoppingToken.IsCancellationRequested)
                await GenerateRecurringBillsAsync();
        }
    }

    private async Task GenerateRecurringBillsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var today = DateTime.Today;
        var rules = await db.RecurringRules
            .Where(r => r.IsActive && r.DayOfMonth == today.Day)
            .ToListAsync();

        if (rules.Count == 0) return;

        var yearMonth = today.ToString("yyyy-MM");
        foreach (var rule in rules)
        {
            // 本月是否已生成过
            var exists = await db.Records.AnyAsync(r =>
                r.UserId == rule.UserId &&
                r.CategoryId == rule.CategoryId &&
                r.AccountId == rule.AccountId &&
                r.Note == $"[周期]{rule.Note}" &&
                r.OccurredAt.Year == today.Year &&
                r.OccurredAt.Month == today.Month);

            if (exists) continue;

            var account = await db.FinAccounts.FindAsync(rule.AccountId);
            if (account == null) continue;

            db.Records.Add(new Record
            {
                UserId = rule.UserId,
                AccountId = rule.AccountId,
                CategoryId = rule.CategoryId,
                Amount = rule.Amount,
                Type = RecordType.Expense,
                OccurredAt = today,
                Note = $"[周期]{rule.Note}",
            });
            account.Balance -= rule.Amount;
        }

        await db.SaveChangesAsync();
        logger.LogInformation("周期账单生成完成，共处理 {Count} 条规则", rules.Count);
    }
}
