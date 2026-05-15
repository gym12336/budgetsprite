using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 月度统计：总收支、分类占比、每日趋势、与上月对比
    [HttpGet("month")]
    public async Task<IActionResult> Month([FromQuery] int year, [FromQuery] int month)
    {
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var lastStart = start.AddMonths(-1);

        var records = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
            .Include(r => r.Category)
            .ToListAsync();

        var totalIncome = records.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount);
        var totalExpense = records.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);

        // 上月支出
        var lastMonthExpense = await db.Records
            .Where(r => r.UserId == UserId && r.Type == RecordType.Expense
                && r.OccurredAt >= lastStart && r.OccurredAt < start)
            .SumAsync(r => r.Amount);

        // 分类占比（仅支出）
        var expenseRecords = records.Where(r => r.Type == RecordType.Expense).ToList();
        var categoryStats = expenseRecords
            .GroupBy(r => new { r.CategoryId, r.Category?.Name, r.Category?.Color })
            .Select(g => new CategoryStatItem(
                g.Key.CategoryId,
                g.Key.Name ?? "未知",
                g.Key.Color,
                g.Sum(r => r.Amount),
                totalExpense > 0 ? Math.Round((double)(g.Sum(r => r.Amount) / totalExpense) * 100, 1) : 0
            ))
            .OrderByDescending(x => x.Amount)
            .ToList();

        // 每日趋势
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dailyStats = Enumerable.Range(1, daysInMonth).Select(d =>
        {
            var date = new DateTime(year, month, d);
            var dayRecords = records.Where(r => r.OccurredAt.Date == date).ToList();
            return new DailyStatItem(
                date.ToString("MM-dd"),
                dayRecords.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount),
                dayRecords.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount)
            );
        }).ToList();

        return Ok(new MonthStatsResponse(totalIncome, totalExpense, totalIncome - totalExpense,
            lastMonthExpense, categoryStats, dailyStats));
    }

    // 年度统计：12个月收支趋势 + 全年支出Top分类
    [HttpGet("year")]
    public async Task<IActionResult> Year([FromQuery] int year)
    {
        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year + 1, 1, 1);

        var records = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
            .Include(r => r.Category)
            .ToListAsync();

        // 12个月趋势
        var monthlyStats = Enumerable.Range(1, 12).Select(m =>
        {
            var monthRecords = records.Where(r => r.OccurredAt.Month == m).ToList();
            return new MonthlyStatItem(
                $"{year}-{m:D2}",
                monthRecords.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount),
                monthRecords.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount)
            );
        }).ToList();

        // 全年支出 Top 分类
        var totalExpense = records.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);
        var topCategories = records
            .Where(r => r.Type == RecordType.Expense)
            .GroupBy(r => new { r.CategoryId, r.Category?.Name, r.Category?.Color })
            .Select(g => new CategoryStatItem(
                g.Key.CategoryId,
                g.Key.Name ?? "未知",
                g.Key.Color,
                g.Sum(r => r.Amount),
                totalExpense > 0 ? Math.Round((double)(g.Sum(r => r.Amount) / totalExpense) * 100, 1) : 0
            ))
            .OrderByDescending(x => x.Amount)
            .Take(10)
            .ToList();

        return Ok(new YearStatsResponse(monthlyStats, topCategories));
    }
}
