using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BudgetSprite.Api.Data;
using BudgetSprite.Api.DTOs;
using BudgetSprite.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace BudgetSprite.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(AppDbContext db, IConfiguration config) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest req)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username))
            return BadRequest(new { message = "用户名已存在" });

        var user = new User
        {
            Username = req.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Email = req.Email,
            Nickname = req.Username,
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new { message = "注册成功" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            return Unauthorized(new { message = "用户名或密码错误" });

        var accessToken = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(config.GetValue<int>("Jwt:RefreshExpireDays"));
        await db.SaveChangesAsync();

        return Ok(new LoginResponse(accessToken, refreshToken, new UserDto(user.Id, user.Username, user.Nickname, user.AvatarUrl)));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshRequest req)
    {
        var user = await db.Users.FirstOrDefaultAsync(u =>
            u.RefreshToken == req.RefreshToken && u.RefreshTokenExpiry > DateTime.UtcNow);
        if (user == null)
            return Unauthorized(new { message = "Refresh token 无效或已过期" });

        var accessToken = GenerateAccessToken(user);
        var newRefresh = GenerateRefreshToken();
        user.RefreshToken = newRefresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(config.GetValue<int>("Jwt:RefreshExpireDays"));
        await db.SaveChangesAsync();

        return Ok(new { accessToken, refreshToken = newRefresh });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId);
        if (user == null) return NotFound();
        return Ok(new UserDto(user.Id, user.Username, user.Nickname, user.AvatarUrl));
    }

    private string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(config.GetValue<int>("Jwt:ExpireMinutes")),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken() =>
        Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
        Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
