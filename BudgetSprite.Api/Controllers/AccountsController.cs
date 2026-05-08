using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/accounts")]
[Authorize]
public class AccountsController(AppDbContext db) : ControllerBase
{
    // TODO: 返回当前用户所有账户
    [HttpGet]
    public IActionResult List() =>
        Ok(new List<AccountDto>());

    // TODO: 创建账户
    [HttpPost]
    public IActionResult Create(AccountCreateRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 更新账户
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, AccountCreateRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 删除账户
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        Ok(new { message = "待实现" });

    // TODO: 内部转账（创建两条转账记录并调整余额）
    [HttpPost("transfer")]
    public IActionResult Transfer(TransferRequest req) =>
        Ok(new { message = "待实现" });
}
