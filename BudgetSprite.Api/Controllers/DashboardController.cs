using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var now = DateTime.Now;
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1);
        var lastStart = start.AddMonths(-1);

        // 本月收支
        var monthRecords = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
            .ToListAsync();
        var monthIncome = monthRecords.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount);
        var monthExpense = monthRecords.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);

        // 上月支出
        var lastMonthExpense = await db.Records
            .Where(r => r.UserId == UserId && r.Type == RecordType.Expense
                && r.OccurredAt >= lastStart && r.OccurredAt < start)
            .SumAsync(r => r.Amount);

        // 账户列表
        var accounts = await db.FinAccounts
            .Where(a => a.UserId == UserId)
            .Select(a => new AccountDto(a.Id, a.Name, (byte)a.Type, a.Balance, a.Note))
            .ToListAsync();

        // 本月预算进度
        var yearMonth = now.ToString("yyyy-MM");
        var budgets = await db.Budgets
            .Where(b => b.UserId == UserId && b.YearMonth == yearMonth)
            .Include(b => b.Category)
            .ToListAsync();

        var expenseByCategory = monthRecords
            .Where(r => r.Type == RecordType.Expense)
            .GroupBy(r => r.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        var budgetDtos = budgets.Select(b =>
        {
            var used = b.CategoryId == null
                ? monthExpense
                : expenseByCategory.GetValueOrDefault(b.CategoryId.Value, 0);
            return new BudgetDto(b.Id, b.CategoryId, b.Category?.Name, b.YearMonth,
                b.Amount, used, b.Amount - used);
        }).ToList();

        // 最近 5 条账单
        var recentRecords = await db.Records
            .Where(r => r.UserId == UserId)
            .Include(r => r.Category)
            .Include(r => r.Account)
            .Include(r => r.Images)
            .OrderByDescending(r => r.OccurredAt)
            .Take(5)
            .Select(r => new RecordDto(
                r.Id, r.AccountId, r.Account.Name,
                r.CategoryId, r.Category.Name, r.Category.Icon, r.Category.Color,
                r.Amount, (byte)r.Type, r.OccurredAt, r.Note, r.Tags,
                r.Images.Select(i => i.ImageUrl).ToList()
            ))
            .ToListAsync();

        return Ok(new DashboardResponse(monthIncome, monthExpense, monthIncome - monthExpense,
            lastMonthExpense, accounts, budgetDtos, recentRecords));
    }
}
