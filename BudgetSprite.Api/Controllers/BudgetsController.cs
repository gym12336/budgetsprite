using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 返回指定月份预算列表，附带已使用金额
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string yearMonth)
    {
        if (string.IsNullOrWhiteSpace(yearMonth))
            yearMonth = DateTime.Now.ToString("yyyy-MM");

        var budgets = await db.Budgets
            .Where(b => b.UserId == UserId && b.YearMonth == yearMonth)
            .Include(b => b.Category)
            .ToListAsync();

        // 解析月份范围
        var start = DateTime.Parse(yearMonth + "-01");
        var end = start.AddMonths(1);

        // 统计该月支出（排除转账）
        var expenseByCategory = await db.Records
            .Where(r => r.UserId == UserId && r.Type == RecordType.Expense
                && r.OccurredAt >= start && r.OccurredAt < end)
            .GroupBy(r => r.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(r => r.Amount) })
            .ToListAsync();

        var totalExpense = expenseByCategory.Sum(x => x.Total);

        var result = budgets.Select(b =>
        {
            var used = b.CategoryId == null
                ? totalExpense
                : expenseByCategory.FirstOrDefault(e => e.CategoryId == b.CategoryId)?.Total ?? 0;
            return new BudgetDto(b.Id, b.CategoryId, b.Category?.Name, b.YearMonth,
                b.Amount, used, b.Amount - used);
        }).ToList();

        return Ok(result);
    }

    // 新增或更新预算（同月同分类则覆盖）
    [HttpPost]
    public async Task<IActionResult> Upsert(BudgetUpsertRequest req)
    {
        var existing = await db.Budgets.FirstOrDefaultAsync(b =>
            b.UserId == UserId && b.YearMonth == req.YearMonth && b.CategoryId == req.CategoryId);

        if (existing != null)
        {
            existing.Amount = req.Amount;
        }
        else
        {
            db.Budgets.Add(new Budget
            {
                UserId = UserId,
                CategoryId = req.CategoryId,
                YearMonth = req.YearMonth,
                Amount = req.Amount,
            });
        }
        await db.SaveChangesAsync();
        return Ok(new { message = existing != null ? "更新成功" : "创建成功" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var budget = await db.Budgets.FirstOrDefaultAsync(b => b.Id == id && b.UserId == UserId);
        if (budget == null) return NotFound(new { message = "预算不存在" });
        db.Budgets.Remove(budget);
        await db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }
}
