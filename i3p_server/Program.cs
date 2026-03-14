using Scalar.AspNetCore; 
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;
using Microsoft.IdentityModel.Tokens;
using i3p_server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;

var builder = WebApplication.CreateBuilder(args);



var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"]   ?? "i3p-server",
            ValidAudience            = builder.Configuration["Jwt:Audience"] ?? "i3p-client",
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            // Map the standard "sub" claim → ClaimTypes.NameIdentifier
            // (required for User.FindFirstValue(ClaimTypes.NameIdentifier) to work)
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
        };
    });

builder.Services.AddAuthorization();

//policy.WithOrigins("http://localhost:4321")
builder.Services.AddCors(options => {
    options.AddDefaultPolicy(policy => {
        policy.AllowAnyOrigin() // Your Astro URL
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add services to the container.


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddOpenApi();

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); //Scalar injection
}


app.UseCors();
app.UseStaticFiles();


app.UseAuthentication();
app.UseAuthorization();



app.UseHttpsRedirection();
app.MapControllers();

app.Run();