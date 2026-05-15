using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // 返回当前用户分类 + 系统预设分类，组装二级树
    [HttpGet]
    public async Task<IActionResult> List()
    {
        var all = await db.Categories
            .Where(c => c.UserId == UserId || c.UserId == null)
            .OrderBy(c => c.SortOrder)
            .ToListAsync();

        // 只取顶级分类，子分类挂在 Children 下
        var roots = all.Where(c => c.ParentId == null)
            .Select(c => BuildDto(c, all))
            .ToList();

        return Ok(roots);
    }

    // 新增自定义分类
    [HttpPost]
    public async Task<IActionResult> Create(CategoryCreateRequest req)
    {
        var category = new Category
        {
            UserId = UserId,
            Name = req.Name,
            ParentId = req.ParentId,
            Icon = req.Icon,
            Color = req.Color,
            Type = (RecordType)req.Type,
            SortOrder = await db.Categories.CountAsync(c => c.UserId == UserId) + 100,
        };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Ok(new { id = category.Id, message = "创建成功" });
    }

    // 更新分类（只允许修改自己的）
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CategoryCreateRequest req)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (cat == null) return NotFound(new { message = "分类不存在或无权修改" });

        cat.Name = req.Name;
        cat.Icon = req.Icon;
        cat.Color = req.Color;
        cat.Type = (RecordType)req.Type;
        await db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    // 删除分类（系统预设不可删）
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var cat = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == UserId);
        if (cat == null) return NotFound(new { message = "分类不存在或系统分类不可删除" });

        var inUse = await db.Records.AnyAsync(r => r.CategoryId == id);
        if (inUse) return BadRequest(new { message = "该分类下有账单记录，无法删除" });

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }

    private static CategoryDto BuildDto(Category c, List<Category> all) => new(
        c.Id, c.Name, c.ParentId, c.Icon, c.Color, (byte)c.Type, c.SortOrder,
        all.Where(x => x.ParentId == c.Id).Select(x => BuildDto(x, all)).ToList()
    );
}
