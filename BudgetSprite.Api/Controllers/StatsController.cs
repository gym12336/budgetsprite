using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(AppDbContext db, IMemoryCache cache) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("month")]
    public async Task<IActionResult> Month([FromQuery] int year, [FromQuery] int month)
    {
        var cacheKey = $"stats:month:{UserId}:{year}:{month}";
        if (cache.TryGetValue(cacheKey, out MonthStatsResponse? cached))
            return Ok(cached);

        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var lastStart = start.AddMonths(-1);

        var records = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
            .Include(r => r.Category)
            .ToListAsync();

        var totalIncome = records.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount);
        var totalExpense = records.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);

        var lastMonthExpense = await db.Records
            .Where(r => r.UserId == UserId && r.Type == RecordType.Expense
                && r.OccurredAt >= lastStart && r.OccurredAt < start)
            .SumAsync(r => r.Amount);

        var expenseRecords = records.Where(r => r.Type == RecordType.Expense).ToList();
        var categoryStats = expenseRecords
            .GroupBy(r => new { r.CategoryId, r.Category?.Name, r.Category?.Color })
            .Select(g => new CategoryStatItem(
                g.Key.CategoryId, g.Key.Name ?? "未知", g.Key.Color,
                g.Sum(r => r.Amount),
                totalExpense > 0 ? Math.Round((double)(g.Sum(r => r.Amount) / totalExpense) * 100, 1) : 0))
            .OrderByDescending(x => x.Amount).ToList();

        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dailyStats = Enumerable.Range(1, daysInMonth).Select(d =>
        {
            var date = new DateTime(year, month, d);
            var dayRecords = records.Where(r => r.OccurredAt.Date == date).ToList();
            return new DailyStatItem(
                date.ToString("MM-dd"),
                dayRecords.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount),
                dayRecords.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount));
        }).ToList();

        var result = new MonthStatsResponse(totalIncome, totalExpense, totalIncome - totalExpense,
            lastMonthExpense, categoryStats, dailyStats);

        cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return Ok(result);
    }

    [HttpGet("year")]
    public async Task<IActionResult> Year([FromQuery] int year)
    {
        var cacheKey = $"stats:year:{UserId}:{year}";
        if (cache.TryGetValue(cacheKey, out YearStatsResponse? cached))
            return Ok(cached);

        var start = new DateTime(year, 1, 1);
        var end = new DateTime(year + 1, 1, 1);

        var records = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
            .Include(r => r.Category)
            .ToListAsync();

        var monthlyStats = Enumerable.Range(1, 12).Select(m =>
        {
            var monthRecords = records.Where(r => r.OccurredAt.Month == m).ToList();
            return new MonthlyStatItem(
                $"{year}-{m:D2}",
                monthRecords.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount),
                monthRecords.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount));
        }).ToList();

        var totalExpense = records.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);
        var topCategories = records.Where(r => r.Type == RecordType.Expense)
            .GroupBy(r => new { r.CategoryId, r.Category?.Name, r.Category?.Color })
            .Select(g => new CategoryStatItem(
                g.Key.CategoryId, g.Key.Name ?? "未知", g.Key.Color,
                g.Sum(r => r.Amount),
                totalExpense > 0 ? Math.Round((double)(g.Sum(r => r.Amount) / totalExpense) * 100, 1) : 0))
            .OrderByDescending(x => x.Amount).Take(10).ToList();

        var result = new YearStatsResponse(monthlyStats, topCategories);
        cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return Ok(result);
    }
}

