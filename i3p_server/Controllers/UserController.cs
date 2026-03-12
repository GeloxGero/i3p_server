
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


using i3p_server.Models;
using i3p_server.Services;

namespace i3p_server.Controllers;


[Route("api/user")]
[ApiController]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly AuthService _authService;

    public UsersController(AppDbContext context, AuthService authService)
    {
        _context = context;
        _authService = authService;
    }

    [HttpGet("GetUsers")]
    public async Task<IActionResult> GetUsers()
    {
        // .ToListAsync() is required to actually execute the query against PostgreSQL
        var result = await _context.Users
            .Select(x => new 
            {
                x.Name, // Matches the property name in your Model
                x.Authority,
                x.Email,
                x.DateCreated,
                x.DateUpdated,
                // Admin or Users
            })
            .ToListAsync();
    
        return Ok(result);
    }
    
    [HttpGet("{id}")] // This defines the route as api/user/5
    public async Task<IActionResult> GetUserById(int id)
    {
        // FindAsync is optimized for looking up primary keys
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound(new { message = $"User with ID {id} not found" });
        }

        // Return the user data (consider excluding the PasswordHash for security)
        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Authority,
            user.Photo,
            user.DateCreated
        });
    }
    
    [HttpGet("GetProfile")]
    public async Task<IActionResult> GetProfile()
    {
        // Extract the User ID from the JWT NameIdentifier claim
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim))
            return Unauthorized();

        var user = await _context.Users.FindAsync(int.Parse(userIdClaim));

        if (user == null) return NotFound();

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            user.Authority,
            user.Photo,
            user.DateCreated
        });
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login([FromBody] LoginDto login)
    {
        Console.WriteLine(login.email.Trim());
        Console.WriteLine(login.email);
        var user = await _context.Users
            .FromSqlRaw("SELECT * FROM Users WHERE Email = {0}", login.email.Trim())
            .FirstOrDefaultAsync();
        
        if (user == null)
        {
            Console.WriteLine("ASFASF");
            return Unauthorized("Invalid asf");
        };

        // 2. Verify password hash
        bool isValid = BCrypt.Net.BCrypt.Verify(login.password, user.PasswordHash);
        if (!isValid)
        {
            Console.WriteLine("Invalid Password");
            return Unauthorized("Invalid credentials");
        };

        // 3. Generate Token
        var token = _authService.GenerateToken(user);

        return Ok(new { 
            token = token,
            user = new { user.Name, user.Email, user.Authority }
        });
    }
    
    [HttpPost("CreateUser")]
    public async Task<IActionResult> Register([FromBody] Users user)
    {
        // 1. Check if user exists
        if (await _context.Users.AnyAsync(u => u.Email == user.Email))
            return BadRequest(new { message = "Email already registered" });

        // 2. Hash the password
        // Note: BCrypt.HashPassword automatically handles salt generation internally
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.PasswordHash);

        // 3. Map the full object to match your updated PostgreSQL schema
        var newUser = new Users 
        { 
            Name = user.Name,
            Email = user.Email, 
            PasswordHash = hashedPassword,
            Authority = user.Authority, // Defaults to Normal if not provided
            Photo = user.Photo,
            DateCreated = DateTime.UtcNow,
            DateUpdated = DateTime.UtcNow
        };

        try 
        {
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User registered successfully" });
        }
        catch (DbUpdateException ex)
        {
            // Helpful for debugging schema mismatches in pgAdmin
            return StatusCode(500, new { message = "Database error", detail = ex.InnerException?.Message });
        }
    }

    [HttpPut("UpdateUser")]
    public async Task<IActionResult> UpdateUsers([FromBody] Users user)
    {
        var rows = await _context.Users.Where(x => x.Id == user.Id)
            .ExecuteUpdateAsync(x => x.SetProperty(x => x.Name, user.Name));

        return Ok(user);
    }
}