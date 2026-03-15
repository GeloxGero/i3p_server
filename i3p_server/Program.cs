using Scalar.AspNetCore; 
using Microsoft.EntityFrameworkCore;
using i3p_server.Models;
using Microsoft.IdentityModel.Tokens;
using i3p_server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

var builder = WebApplication.CreateBuilder(args);



var jwtKey = builder.Configuration["Jwt:Key"]
             ?? throw new InvalidOperationException("Jwt:Key is missing from appsettings.json");


builder.Services.AddHttpClient();
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy => policy.WithOrigins("https://i3p.onrender.com", "https://i3p-1.onrender.com")
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Register Cloudinary
var account = new Account(
    builder.Configuration["Cloudinary:CloudName"],
    builder.Configuration["Cloudinary:ApiKey"],
    builder.Configuration["Cloudinary:ApiSecret"]
);
builder.Services.AddSingleton(new Cloudinary(account));



builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddDbContext<AppDbContext>(options => 
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"), npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
        maxRetryCount: 5,
        maxRetryDelay: TimeSpan.FromSeconds(10),
        errorCodesToAdd: null)
        ));


builder.Services.AddOpenApi();

builder.Services.AddScoped<AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(); //Scalar injection
}


app.UseRouting();

app.UseCors("AllowFrontend");
app.UseCors("AllowStaticSite");
app.UseStaticFiles();




app.UseAuthentication();
app.UseAuthorization();



app.UseHttpsRedirection();
app.MapControllers();


app.Run();