using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiAnalysisController(
    AppDbContext db,
    IMemoryCache cache,
    IConfiguration config,
    IHttpClientFactory httpFactory) : ControllerBase
{
    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("analysis")]
    public async Task<IActionResult> Analysis([FromQuery] int year, [FromQuery] int month)
    {
        var cacheKey = $"ai:analysis:{UserId}:{year}:{month}";
        if (cache.TryGetValue(cacheKey, out AiAnalysisResponse? cached))
            return Ok(cached);

        // 收集数据
        var start = new DateTime(year, month, 1);
        var end = start.AddMonths(1);
        var lastStart = start.AddMonths(-1);
        var lastYearStart = start.AddYears(-1);
        var lastYearEnd = lastYearStart.AddMonths(1);

        var thisMonth = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
            .Include(r => r.Category).ToListAsync();

        var lastMonth = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= lastStart && r.OccurredAt < start)
            .ToListAsync();

        var lastYear = await db.Records
            .Where(r => r.UserId == UserId && r.OccurredAt >= lastYearStart && r.OccurredAt < lastYearEnd)
            .ToListAsync();

        var budgets = await db.Budgets
            .Where(b => b.UserId == UserId && b.YearMonth == $"{year}-{month:D2}")
            .Include(b => b.Category).ToListAsync();

        // 计算关键指标
        var income = thisMonth.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount);
        var expense = thisMonth.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);
        var lastExpense = lastMonth.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);
        var lastYearExpense = lastYear.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);

        var topCategory = thisMonth
            .Where(r => r.Type == RecordType.Expense)
            .GroupBy(r => new { r.CategoryId, r.Category?.Name })
            .Select(g => new { g.Key.Name, Total = g.Sum(r => r.Amount) })
            .OrderByDescending(x => x.Total).FirstOrDefault();

        var lastTopCategory = lastMonth
            .Where(r => r.Type == RecordType.Expense)
            .GroupBy(r => r.CategoryId)
            .Select(g => new { CategoryId = g.Key, Total = g.Sum(r => r.Amount) })
            .OrderByDescending(x => x.Total).FirstOrDefault();

        var peakDay = thisMonth
            .Where(r => r.Type == RecordType.Expense)
            .GroupBy(r => r.OccurredAt.Date)
            .Select(g => new { Date = g.Key, Total = g.Sum(r => r.Amount) })
            .OrderByDescending(x => x.Total).FirstOrDefault();

        var overBudgets = new List<string>();
        var warnBudgets = new List<string>();
        foreach (var b in budgets)
        {
            var used = b.CategoryId == null
                ? expense
                : thisMonth.Where(r => r.Type == RecordType.Expense && r.CategoryId == b.CategoryId).Sum(r => r.Amount);
            if (used >= b.Amount) overBudgets.Add(b.Category?.Name ?? "总预算");
            else if (used / b.Amount >= 0.8m) warnBudgets.Add(b.Category?.Name ?? "总预算");
        }

        // 构建给 DeepSeek 的数据摘要 prompt
        var sb = new StringBuilder();
        sb.AppendLine($"用户 {year}年{month}月 财务数据：");
        sb.AppendLine($"- 本月收入：¥{income:F2}，支出：¥{expense:F2}，结余：¥{income - expense:F2}");
        sb.AppendLine($"- 上月支出：¥{lastExpense:F2}");
        sb.AppendLine($"- 去年同月支出：¥{lastYearExpense:F2}");
        if (topCategory != null)
            sb.AppendLine($"- 最大支出分类：{topCategory.Name}（¥{topCategory.Total:F2}，占{(expense > 0 ? topCategory.Total / expense * 100 : 0):F1}%）");
        if (peakDay != null)
            sb.AppendLine($"- 消费高峰日：{peakDay.Date:M月d日}（¥{peakDay.Total:F2}）");
        if (overBudgets.Any())
            sb.AppendLine($"- 超支预算：{string.Join("、", overBudgets)}");
        if (warnBudgets.Any())
            sb.AppendLine($"- 接近上限预算（≥80%）：{string.Join("、", warnBudgets)}");
        if (budgets.Count == 0)
            sb.AppendLine("- 本月未设置预算");

        sb.AppendLine();
        sb.AppendLine("请根据以上数据，以JSON格式返回财务分析报告，格式如下（不要返回其他内容）：");
        sb.AppendLine("{");
        sb.AppendLine("  \"summary\": \"一句话整体评价（20字以内，可含emoji）\",");
        sb.AppendLine("  \"insights\": [");
        sb.AppendLine("    {\"type\":\"trend\",\"icon\":\"📈\",\"title\":\"消费趋势\",\"content\":\"具体分析...\",\"level\":\"good|warning|danger|info\"},");
        sb.AppendLine("    ... 共5-7条洞察");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine("level说明：good=绿色正面，warning=橙色提醒，danger=红色警告，info=蓝色信息");
        sb.AppendLine("洞察类型包括：trend(趋势)、category(分类)、budget(预算)、peak(高峰)、saving(储蓄)、compare(同比)、advice(建议)、savings(节省建议)");
        sb.AppendLine("【重要】必须包含一条 type=\"savings\" 的洞察，给出本月最具体可执行的节省建议，要有明确的金额数字，格式如：如果减少XX分类支出¥XX，每月可节省¥XX");

        // 调用 DeepSeek
        var content = await CallDeepSeek(sb.ToString());
        if (content == null)
            return StatusCode(500, new { message = "AI 服务调用失败" });

        // 解析 AI 返回的 JSON
        AiAnalysisResponse result;
        try
        {
            using var aiDoc = JsonDocument.Parse(content);
            var root = aiDoc.RootElement;
            var summary = root.GetProperty("summary").GetString() ?? "分析完成";
            var insights = root.GetProperty("insights").EnumerateArray()
                .Select(i => new AiInsight(
                    i.GetProperty("type").GetString() ?? "info",
                    i.GetProperty("icon").GetString() ?? "💡",
                    i.GetProperty("title").GetString() ?? "",
                    i.GetProperty("content").GetString() ?? "",
                    i.GetProperty("level").GetString() ?? "info"
                )).ToList();
            result = new AiAnalysisResponse(summary, insights);
        }
        catch
        {
            result = new AiAnalysisResponse("分析完成", [
                new AiInsight("info", "💡", "AI 分析", content, "info")
            ]);
        }

        cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return Ok(result);
    }

    // ── 自然语言记账解析 ────────────────────────────────────────
    [HttpPost("parse-record")]
    public async Task<IActionResult> ParseRecord(ParseRecordRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Text))
            return BadRequest(new { message = "文本不能为空" });

        var today = DateTime.Today.ToString("yyyy-MM-dd");
        var prompt = $@"今天是{today}。请从以下文本中提取记账信息，返回JSON格式（不要返回其他内容）：
文本：{req.Text}

返回格式：
{{
  ""type"": 0,           // 0=支出 1=收入，无法判断默认0
  ""amount"": 35.00,     // 金额数字，无法识别则null
  ""categoryHint"": ""餐饮"", // 推测的消费分类中文名，如餐饮/交通/购物/娱乐/医疗/工资/副业等
  ""note"": ""外卖"",     // 备注描述
  ""dateHint"": ""{today}"" // 日期yyyy-MM-dd，今天/默认用{today}，昨天减1天，其余按语义推断
}}";

        var result = await CallDeepSeek(prompt, maxTokens: 200);
        if (result == null)
            return StatusCode(500, new { message = "AI 服务调用失败" });

        try
        {
            using var doc = JsonDocument.Parse(result);
            var root = doc.RootElement;
            return Ok(new ParseRecordResult(
                root.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.Number ? (byte?)t.GetByte() : null,
                root.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number ? (decimal?)a.GetDecimal() : null,
                root.TryGetProperty("categoryHint", out var c) ? c.GetString() : null,
                root.TryGetProperty("note", out var n) ? n.GetString() : null,
                root.TryGetProperty("dateHint", out var d) ? d.GetString() : null,
                true, null
            ));
        }
        catch
        {
            return Ok(new ParseRecordResult(null, null, null, null, null, false, "无法识别，请手动填写"));
        }
    }

    // ── 多月趋势分析 ────────────────────────────────────────────
    [HttpGet("trend-analysis")]
    public async Task<IActionResult> TrendAnalysis([FromQuery] int months = 3)
    {
        if (months < 2 || months > 12) months = 3;
        var now = DateTime.Now;
        var cacheKey = $"ai:trend:{UserId}:{months}:{now:yyyyMM}";
        if (cache.TryGetValue(cacheKey, out AiAnalysisResponse? cached))
            return Ok(cached);

        var sb = new StringBuilder();
        sb.AppendLine($"用户最近{months}个月的财务趋势数据：");

        for (int i = months - 1; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var start = new DateTime(date.Year, date.Month, 1);
            var end = start.AddMonths(1);

            var records = await db.Records
                .Where(r => r.UserId == UserId && r.OccurredAt >= start && r.OccurredAt < end)
                .Include(r => r.Category).ToListAsync();

            var income = records.Where(r => r.Type == RecordType.Income).Sum(r => r.Amount);
            var expense = records.Where(r => r.Type == RecordType.Expense).Sum(r => r.Amount);
            var topCat = records.Where(r => r.Type == RecordType.Expense)
                .GroupBy(r => r.Category?.Name ?? "未知")
                .Select(g => new { Name = g.Key, Total = g.Sum(r => r.Amount) })
                .OrderByDescending(x => x.Total).FirstOrDefault();

            sb.AppendLine($"- {date:yyyy年M月}：收入¥{income:F0}，支出¥{expense:F0}，结余¥{income - expense:F0}" +
                (topCat != null ? $"，主要支出：{topCat.Name}(¥{topCat.Total:F0})" : ""));
        }

        sb.AppendLine();
        sb.AppendLine("请根据以上多月趋势数据，以JSON格式返回趋势分析报告（不要返回其他内容）：");
        sb.AppendLine("{");
        sb.AppendLine("  \"summary\": \"一句话整体趋势评价（20字以内，可含emoji）\",");
        sb.AppendLine("  \"insights\": [");
        sb.AppendLine("    {\"type\":\"trend\",\"icon\":\"📈\",\"title\":\"支出趋势\",\"content\":\"跨月趋势分析...\",\"level\":\"good|warning|danger|info\"},");
        sb.AppendLine("    ... 共4-6条，侧重趋势变化和预测");
        sb.AppendLine("  ]");
        sb.AppendLine("}");
        sb.AppendLine("必须包含一条 type=savings 的节省建议洞察");

        var content = await CallDeepSeek(sb.ToString(), maxTokens: 1000);
        if (content == null)
            return StatusCode(500, new { message = "AI 服务调用失败" });

        AiAnalysisResponse result;
        try
        {
            using var aiDoc = JsonDocument.Parse(content);
            var root = aiDoc.RootElement;
            var summary = root.GetProperty("summary").GetString() ?? "趋势分析完成";
            var insights = root.GetProperty("insights").EnumerateArray()
                .Select(i => new AiInsight(
                    i.GetProperty("type").GetString() ?? "info",
                    i.GetProperty("icon").GetString() ?? "💡",
                    i.GetProperty("title").GetString() ?? "",
                    i.GetProperty("content").GetString() ?? "",
                    i.GetProperty("level").GetString() ?? "info"
                )).ToList();
            result = new AiAnalysisResponse(summary, insights);
        }
        catch
        {
            result = new AiAnalysisResponse("趋势分析完成", [new AiInsight("info", "💡", "趋势分析", content, "info")]);
        }

        cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        return Ok(result);
    }

    // ── 公共 DeepSeek 调用 ─────────────────────────────────────
    private async Task<string?> CallDeepSeek(string prompt, int maxTokens = 1000)
    {
        var client = httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config["DeepSeek:ApiKey"]!);

        var body = JsonSerializer.Serialize(new
        {
            model = config["DeepSeek:Model"]!,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.7,
            max_tokens = maxTokens,
            response_format = new { type = "json_object" }
        });

        var response = await client.PostAsync(
            $"{config["DeepSeek:BaseUrl"]}/chat/completions",
            new StringContent(body, Encoding.UTF8, "application/json"));

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }
}
