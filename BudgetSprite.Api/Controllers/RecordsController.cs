using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/records")]
[Authorize]
public class RecordsController(AppDbContext db) : ControllerBase
{
    // TODO: 实现账单列表（分页、筛选）
    [HttpGet]
    public IActionResult List([FromQuery] RecordQueryRequest req) =>
        Ok(new PagedResult<RecordDto>([], 0, req.Page, req.PageSize));

    // TODO: 实现新建账单
    [HttpPost]
    public IActionResult Create(RecordCreateRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 实现更新账单
    [HttpPut("{id:long}")]
    public IActionResult Update(long id, RecordUpdateRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 实现删除账单
    [HttpDelete("{id:long}")]
    public IActionResult Delete(long id) =>
        Ok(new { message = "待实现" });

    // TODO: 实现批量删除
    [HttpDelete("batch")]
    public IActionResult BatchDelete([FromBody] BatchDeleteRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 实现 CSV/Excel 导入
    [HttpPost("import")]
    public IActionResult Import(IFormFile file) =>
        Ok(new { message = "待实现" });

    // TODO: 实现导出
    [HttpGet("export")]
    public IActionResult Export([FromQuery] RecordQueryRequest req) =>
        Ok(new { message = "待实现" });
}

public record BatchDeleteRequest(List<long> Ids);
