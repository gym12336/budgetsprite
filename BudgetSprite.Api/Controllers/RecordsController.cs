using System.Security.Claims;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/records")]
[Authorize]
public class RecordsController(AppDbContext db) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // ── 列表（分页 + 多条件筛选） ──────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] RecordQueryRequest req)
    {
        var query = db.Records
            .Where(r => r.UserId == UserId)
            .Include(r => r.Category)
            .Include(r => r.Account)
            .Include(r => r.Images)
            .AsQueryable();

        if (req.Type.HasValue)
            query = query.Where(r => (byte)r.Type == req.Type.Value);
        if (req.CategoryId.HasValue)
            query = query.Where(r => r.CategoryId == req.CategoryId.Value);
        if (req.AccountId.HasValue)
            query = query.Where(r => r.AccountId == req.AccountId.Value);
        if (!string.IsNullOrWhiteSpace(req.Keyword))
            query = query.Where(r =>
                (r.Note != null && r.Note.Contains(req.Keyword)) ||
                (r.Tags != null && r.Tags.Contains(req.Keyword)));
        if (req.StartDate.HasValue)
            query = query.Where(r => r.OccurredAt >= req.StartDate.Value);
        if (req.EndDate.HasValue)
            query = query.Where(r => r.OccurredAt <= req.EndDate.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.OccurredAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .Select(r => ToDto(r))
            .ToListAsync();

        return Ok(new PagedResult<RecordDto>(items, total, req.Page, req.PageSize));
    }

    // ── 新增账单 ───────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create(RecordCreateRequest req)
    {
        var account = await db.FinAccounts.FirstOrDefaultAsync(a => a.Id == req.AccountId && a.UserId == UserId);
        if (account == null) return BadRequest(new { message = "账户不存在" });

        var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == req.CategoryId &&
            (c.UserId == UserId || c.UserId == null));
        if (category == null) return BadRequest(new { message = "分类不存在" });

        var record = new Record
        {
            UserId = UserId,
            AccountId = req.AccountId,
            CategoryId = req.CategoryId,
            Amount = req.Amount,
            Type = (RecordType)req.Type,
            OccurredAt = req.OccurredAt,
            Note = req.Note,
            Tags = req.Tags,
        };
        db.Records.Add(record);

        // 同步账户余额
        AdjustBalance(account, record.Type, record.Amount, add: true);

        await db.SaveChangesAsync();
        return Ok(new { id = record.Id, message = "创建成功" });
    }

    // ── 更新账单 ───────────────────────────────────────────────────
    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, RecordUpdateRequest req)
    {
        var record = await db.Records
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (record == null) return NotFound(new { message = "账单不存在" });

        var newAccount = await db.FinAccounts.FirstOrDefaultAsync(a => a.Id == req.AccountId && a.UserId == UserId);
        if (newAccount == null) return BadRequest(new { message = "账户不存在" });

        // 回滚旧余额
        AdjustBalance(record.Account, record.Type, record.Amount, add: false);
        // 若换了账户，旧账户已回滚，新账户加上新金额
        if (record.AccountId != req.AccountId)
        {
            record.AccountId = req.AccountId;
            record.Account = newAccount;
        }

        record.CategoryId = req.CategoryId;
        record.Amount = req.Amount;
        record.Type = (RecordType)req.Type;
        record.OccurredAt = req.OccurredAt;
        record.Note = req.Note;
        record.Tags = req.Tags;
        record.UpdatedAt = DateTime.UtcNow;

        // 应用新余额
        AdjustBalance(newAccount, record.Type, record.Amount, add: true);

        await db.SaveChangesAsync();
        return Ok(new { message = "更新成功" });
    }

    // ── 删除账单（软删除） ─────────────────────────────────────────
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var record = await db.Records
            .Include(r => r.Account)
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == UserId);
        if (record == null) return NotFound(new { message = "账单不存在" });

        record.IsDeleted = true;
        record.UpdatedAt = DateTime.UtcNow;
        AdjustBalance(record.Account, record.Type, record.Amount, add: false);

        await db.SaveChangesAsync();
        return Ok(new { message = "删除成功" });
    }

    // ── 批量删除 ──────────────────────────────────────────────────
    [HttpDelete("batch")]
    public async Task<IActionResult> BatchDelete([FromBody] BatchDeleteRequest req)
    {
        var records = await db.Records
            .Include(r => r.Account)
            .Where(r => req.Ids.Contains(r.Id) && r.UserId == UserId)
            .ToListAsync();

        foreach (var r in records)
        {
            r.IsDeleted = true;
            r.UpdatedAt = DateTime.UtcNow;
            AdjustBalance(r.Account, r.Type, r.Amount, add: false);
        }
        await db.SaveChangesAsync();
        return Ok(new { message = $"已删除 {records.Count} 条" });
    }

    // ── 导出 Excel ────────────────────────────────────────────────
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] RecordQueryRequest req)
    {
        var query = db.Records
            .Where(r => r.UserId == UserId)
            .Include(r => r.Category)
            .Include(r => r.Account)
            .AsQueryable();

        if (req.Type.HasValue) query = query.Where(r => (byte)r.Type == req.Type.Value);
        if (req.CategoryId.HasValue) query = query.Where(r => r.CategoryId == req.CategoryId.Value);
        if (req.AccountId.HasValue) query = query.Where(r => r.AccountId == req.AccountId.Value);
        if (!string.IsNullOrWhiteSpace(req.Keyword))
            query = query.Where(r =>
                (r.Note != null && r.Note.Contains(req.Keyword)) ||
                (r.Tags != null && r.Tags.Contains(req.Keyword)));
        if (req.StartDate.HasValue) query = query.Where(r => r.OccurredAt >= req.StartDate.Value);
        if (req.EndDate.HasValue) query = query.Where(r => r.OccurredAt <= req.EndDate.Value);

        var records = await query.OrderByDescending(r => r.OccurredAt).ToListAsync();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("账单");
        string[] headers = ["日期", "类型", "金额", "分类", "账户", "备注", "标签"];
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
            ws.Cell(1, i + 1).Style.Font.Bold = true;
        }

        string[] typeNames = ["支出", "收入", "转账"];
        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            ws.Cell(i + 2, 1).Value = r.OccurredAt.ToString("yyyy-MM-dd HH:mm");
            ws.Cell(i + 2, 2).Value = typeNames[(int)r.Type];
            ws.Cell(i + 2, 3).Value = (double)r.Amount;
            ws.Cell(i + 2, 4).Value = r.Category?.Name ?? "";
            ws.Cell(i + 2, 5).Value = r.Account?.Name ?? "";
            ws.Cell(i + 2, 6).Value = r.Note ?? "";
            ws.Cell(i + 2, 7).Value = r.Tags ?? "";
        }
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"账单_{DateTime.Now:yyyyMMdd}.xlsx");
    }

    // ── 导入 CSV / Excel ──────────────────────────────────────────
    [HttpPost("import")]
    public async Task<IActionResult> Import(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "请选择文件" });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (ext != ".csv" && ext != ".xlsx" && ext != ".xls")
            return BadRequest(new { message = "仅支持 CSV / Excel 文件" });

        // 取第一个账户作为默认账户（若无则报错）
        var defaultAccount = await db.FinAccounts.FirstOrDefaultAsync(a => a.UserId == UserId);
        if (defaultAccount == null)
            return BadRequest(new { message = "请先创建至少一个账户再导入" });

        // 取默认支出分类
        var defaultCategory = await db.Categories
            .FirstOrDefaultAsync(c => (c.UserId == UserId || c.UserId == null) && (int)c.Type == 0);
        if (defaultCategory == null)
            return BadRequest(new { message = "请先创建至少一个分类再导入" });

        var rows = new List<(DateTime date, byte type, decimal amount, string note, string tags)>();
        var errors = new List<string>();

        using var stream = file.OpenReadStream();

        if (ext == ".csv")
        {
            using var reader = new StreamReader(stream);
            string? line;
            int lineNum = 0;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineNum++;
                if (lineNum == 1) continue; // 跳过表头
                var cols = line.Split(',');
                if (cols.Length < 3) { errors.Add($"第{lineNum}行列数不足"); continue; }
                if (!DateTime.TryParse(cols[0].Trim(), out var date)) { errors.Add($"第{lineNum}行日期格式错误"); continue; }
                var typeStr = cols[1].Trim();
                byte type = typeStr == "收入" ? (byte)1 : typeStr == "转账" ? (byte)2 : (byte)0;
                if (!decimal.TryParse(cols[2].Trim(), out var amount)) { errors.Add($"第{lineNum}行金额格式错误"); continue; }
                var note = cols.Length > 3 ? cols[3].Trim() : "";
                var tags = cols.Length > 4 ? cols[4].Trim() : "";
                rows.Add((date, type, amount, note, tags));
            }
        }
        else
        {
            using var wb = new XLWorkbook(stream);
            var ws = wb.Worksheets.First();
            foreach (var row in ws.RowsUsed().Skip(1))
            {
                int rowNum = row.RowNumber();
                var dateStr = row.Cell(1).GetString().Trim();
                if (!DateTime.TryParse(dateStr, out var date)) { errors.Add($"第{rowNum}行日期格式错误"); continue; }
                var typeStr = row.Cell(2).GetString().Trim();
                byte type = typeStr == "收入" ? (byte)1 : typeStr == "转账" ? (byte)2 : (byte)0;
                if (!decimal.TryParse(row.Cell(3).GetString().Trim(), out var amount)) { errors.Add($"第{rowNum}行金额格式错误"); continue; }
                var note = row.Cell(6).GetString().Trim();
                var tags = row.Cell(7).GetString().Trim();
                rows.Add((date, type, amount, note, tags));
            }
        }

        var records = rows.Select(r => new Record
        {
            UserId = UserId,
            AccountId = defaultAccount.Id,
            CategoryId = defaultCategory.Id,
            Amount = r.amount,
            Type = (RecordType)r.type,
            OccurredAt = r.date,
            Note = r.note,
            Tags = r.tags,
        }).ToList();

        db.Records.AddRange(records);
        await db.SaveChangesAsync();

        return Ok(new
        {
            message = $"导入成功 {records.Count} 条",
            errors = errors.Count > 0 ? errors : null
        });
    }

    // ── 私有工具 ──────────────────────────────────────────────────
    private static void AdjustBalance(FinAccount account, RecordType type, decimal amount, bool add)
    {
        var delta = add ? amount : -amount;
        account.Balance += type == RecordType.Income ? delta : -delta;
    }

    private static RecordDto ToDto(Record r) => new(
        r.Id,
        r.AccountId,
        r.Account?.Name ?? "",
        r.CategoryId,
        r.Category?.Name ?? "",
        r.Category?.Icon,
        r.Category?.Color,
        r.Amount,
        (byte)r.Type,
        r.OccurredAt,
        r.Note,
        r.Tags,
        r.Images?.Select(i => i.ImageUrl).ToList() ?? []
    );
}

public record BatchDeleteRequest(List<long> Ids);
