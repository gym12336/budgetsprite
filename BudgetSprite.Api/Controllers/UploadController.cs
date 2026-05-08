using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/upload")]
[Authorize]
public class UploadController(IConfiguration config, IWebHostEnvironment env) : ControllerBase
{
    // TODO: 保存图片到 wwwroot/uploads，返回可访问 URL
    [HttpPost("image")]
    public async Task<IActionResult> Image(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "请选择文件" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "仅支持 jpg/png/webp 格式" });

        var savePath = config["Upload:SavePath"] ?? "wwwroot/uploads";
        var dir = Path.Combine(env.ContentRootPath, savePath);
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        var url = $"/uploads/{fileName}";
        return Ok(new { url });
    }
}
