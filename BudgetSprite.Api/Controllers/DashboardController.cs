using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController(AppDbContext db) : ControllerBase
{
    // TODO: 返回本月收支、账户列表、预算进度、最近5条账单
    [HttpGet]
    public IActionResult Get() =>
        Ok(new DashboardResponse(0, 0, 0, 0, [], [], []));
}
