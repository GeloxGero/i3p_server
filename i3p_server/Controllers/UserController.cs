using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using i3p_server.Models;
using i3p_server.Services;

namespace i3p_server.Controllers;

[Route("api/user")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService  _authService;

    public UsersController(AppDbContext context, AuthService authService)
    {
        _context     = context;
        _authService = authService;
    }

    // ── GET /api/user/GetUsers ────────────────────────────────────────────────
    [Authorize]
    [HttpGet("GetUsers")]
    public async Task<IActionResult> GetUsers()
    {
        var result = await _context.Users
            .Select(x => new { x.Name, x.Authority, x.Email, x.DateCreated, x.DateUpdated })
            .ToListAsync();
        return Ok(result);
    }

    // ── GET /api/user/{id} ────────────────────────────────────────────────────
    [Authorize]
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
            return NotFound(new { message = $"User with ID {id} not found" });

        return Ok(new { user.Id, user.Name, user.Email, user.Authority, user.Photo, user.DateCreated });
    }

    // ── GET /api/user/GetProfile ──────────────────────────────────────────────
    // Requires a valid JWT. Reads the user ID from the NameIdentifier claim
    // that AuthService stamps into every token.
    [Authorize]
    [HttpGet("GetProfile")]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Invalid or missing token claim." });

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Authority,
            user.Photo,
            user.DateCreated,
        });
    }

    // ── POST /api/user/Login ──────────────────────────────────────────────────
    // Public — no [Authorize]
    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        if (login == null || string.IsNullOrWhiteSpace(login.email))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == login.email.Trim().ToLower());

        if (user == null || !BCrypt.Net.BCrypt.Verify(login.password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });

        var token = _authService.GenerateToken(user);

        return Ok(new
        {
            token,
            user = new { user.Id, user.Name, user.Email, user.Authority }
        });
    }

    // ── POST /api/user/CreateUser ─────────────────────────────────────────────
    // Public — registration
    [HttpPost("CreateUser")]
    public async Task<IActionResult> Register([FromBody] Users user)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash) || user.PasswordHash.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        if (await _context.Users.AnyAsync(u => u.Email.ToLower() == user.Email.ToLower()))
            return BadRequest(new { message = "Email already registered." });

        var newUser = new Users
        {
            Name         = user.Name,
            Email        = user.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash),
            Authority    = user.Authority,
            Photo        = user.Photo,
            DateCreated  = DateTime.UtcNow,
            DateUpdated  = DateTime.UtcNow,
        };

        try
        {
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User registered successfully." });
        }
        catch (DbUpdateException ex)
        {
            return StatusCode(500, new { message = "Database error.", detail = ex.InnerException?.Message });
        }
    }

    // ── PUT /api/user/UpdateUser ──────────────────────────────────────────────
    [Authorize]
    [HttpPut("UpdateUser")]
    public async Task<IActionResult> UpdateUser([FromBody] Users user)
    {
        var rows = await _context.Users
            .Where(x => x.Id == user.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(p => p.Name, user.Name));

        return rows == 0 ? NotFound() : Ok(new { message = "Updated." });
    }
}