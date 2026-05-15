using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var accounts = await db.FinAccounts
            .Where(a => a.UserId == UserId)
            .Select(a => new AccountDto(a.Id, a.Name, (byte)a.Type, a.Balance, a.Note))
            .ToListAsync();
        return Ok(accounts);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AccountCreateRequest req)
    {
        var account = new FinAccount
        {
            UserId = UserId,
            Name = req.Name,
            Type = (AccountType)req.Type,
            Balance = req.Balance,
            Note = req.Note,
        };
        db.FinAccounts.Add(account);
        await db.SaveChangesAsync();
        return Ok(new { id = account.Id, message = "创建成功" });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AccountCreateRequest req)
    {
        var account = await db.FinAccounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);
        if (account == null) return NotFound(new { message = "账户不存在" });

        account.Name = req.Name;
        account.Type = (AccountType)req.Type;
        account.Balance = req.Balance;
        account.Note = req.Note;
        await db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var account = await db.FinAccounts.FirstOrDefaultAsync(a => a.Id == id && a.UserId == UserId);
        if (account == null) return NotFound(new { message = "账户不存在" });

        var inUse = await db.Records.AnyAsync(r => r.AccountId == id);
        if (inUse) return BadRequest(new { message = "该账户下有账单记录，无法删除" });

        db.FinAccounts.Remove(account);
        await db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }

    // 账户间内部转账：创建两条转账记录并调整双方余额
    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer(TransferRequest req)
    {
        var from = await db.FinAccounts.FirstOrDefaultAsync(a => a.Id == req.FromAccountId && a.UserId == UserId);
        var to = await db.FinAccounts.FirstOrDefaultAsync(a => a.Id == req.ToAccountId && a.UserId == UserId);
        if (from == null || to == null) return BadRequest(new { message = "账户不存在" });
        if (from.Balance < req.Amount) return BadRequest(new { message = "账户余额不足" });

        // 取第一个分类作为转账分类占位
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.UserId == null || c.UserId == UserId);
        if (cat == null) return BadRequest(new { message = "请先创建分类" });

        db.Records.AddRange(
            new Record
            {
                UserId = UserId, AccountId = from.Id, CategoryId = cat.Id,
                Amount = req.Amount, Type = RecordType.Transfer,
                OccurredAt = req.OccurredAt, Note = req.Note ?? $"转出至{to.Name}",
            },
            new Record
            {
                UserId = UserId, AccountId = to.Id, CategoryId = cat.Id,
                Amount = req.Amount, Type = RecordType.Transfer,
                OccurredAt = req.OccurredAt, Note = req.Note ?? $"从{from.Name}转入",
            }
        );
        from.Balance -= req.Amount;
        to.Balance += req.Amount;
        await db.SaveChangesAsync();
        return Ok(new { message = "转账成功" });
    }
}
