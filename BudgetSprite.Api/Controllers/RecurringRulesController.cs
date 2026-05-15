using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/recurring")]
[Authorize]
public class RecurringRulesController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var rules = await db.RecurringRules
            .Where(r => r.UserId == UserId)
            .Include(r => r.Category)
            .Include(r => r.Account)
            .Select(r => new RecurringRuleDto(
                r.Id, r.CategoryId, r.Category.Name,
                r.AccountId, r.Account.Name,
                r.Amount, r.Note, r.DayOfMonth, r.IsActive))
            .ToListAsync();
        return Ok(rules);
    }

    [HttpPost]
    public async Task<IActionResult> Create(RecurringRuleRequest req)
    {
        var rule = new RecurringRule
        {
            UserId = UserId,
            CategoryId = req.CategoryId,
            AccountId = req.AccountId,
            Amount = req.Amount,
            Note = req.Note,
            DayOfMonth = req.DayOfMonth,
            IsActive = true,
        };
        db.RecurringRules.Add(rule);
        await db.SaveChangesAsync();
        return Ok(new { id = rule.Id, message = "创建成功" });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, RecurringRuleRequest req)
    {
        var rule = await db.RecurringRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (rule == null) return NotFound(new { message = "规则不存在" });

        rule.CategoryId = req.CategoryId;
        rule.AccountId = req.AccountId;
        rule.Amount = req.Amount;
        rule.Note = req.Note;
        rule.DayOfMonth = req.DayOfMonth;
        await db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var rule = await db.RecurringRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (rule == null) return NotFound(new { message = "规则不存在" });
        db.RecurringRules.Remove(rule);
        await db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }

    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var rule = await db.RecurringRules.FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (rule == null) return NotFound(new { message = "规则不存在" });
        rule.IsActive = !rule.IsActive;
        await db.SaveChangesAsync();
        return Ok(new { isActive = rule.IsActive });
    }
}
