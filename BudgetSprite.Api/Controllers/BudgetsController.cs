using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/budgets")]
[Authorize]
public class BudgetsController(AppDbContext db) : ControllerBase
{
    // TODO: 返回指定月份预算列表（含已使用金额）
    [HttpGet]
    public IActionResult List([FromQuery] string yearMonth) =>
        Ok(new List<BudgetDto>());

    // TODO: 创建或更新预算（同月同分类则覆盖）
    [HttpPost]
    public IActionResult Upsert(BudgetUpsertRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 删除预算
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        Ok(new { message = "待实现" });
}
