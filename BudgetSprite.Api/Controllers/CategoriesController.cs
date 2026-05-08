using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    // TODO: 返回当前用户分类（含系统默认分类），组装二级树
    [HttpGet]
    public IActionResult List() =>
        Ok(new List<CategoryDto>());

    // TODO: 创建自定义分类
    [HttpPost]
    public IActionResult Create(CategoryCreateRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 更新分类
    [HttpPut("{id:int}")]
    public IActionResult Update(int id, CategoryCreateRequest req) =>
        Ok(new { message = "待实现" });

    // TODO: 删除分类（仅自定义，系统分类不可删）
    [HttpDelete("{id:int}")]
    public IActionResult Delete(int id) =>
        Ok(new { message = "待实现" });
}
