using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/stats")]
[Authorize]
public class StatsController(AppDbContext db) : ControllerBase
{
    // TODO: 月度统计（总收支、分类占比、每日趋势）
    [HttpGet("month")]
    public IActionResult Month([FromQuery] int year, [FromQuery] int month) =>
        Ok(new MonthStatsResponse(0, 0, 0, 0, [], []));

    // TODO: 年度统计（12个月趋势、top分类）
    [HttpGet("year")]
    public IActionResult Year([FromQuery] int year) =>
        Ok(new YearStatsResponse([], []));
}
