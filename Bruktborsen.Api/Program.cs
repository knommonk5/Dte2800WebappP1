using Bruktborsen.Api.Data;
using Bruktborsen.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bruktborsen.Api.Data.Seed;
using Bruktborsen.Api.Repositories;
using Bruktborsen.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration
                           .GetConnectionString("BruktborsenDatabase")
                       ?? "Data Source=bruktborsen.db";

builder.Services.AddDbContext<BruktborsenDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BruktborsenDbContext>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ICategoryService, CategoryService>();

var app = builder.Build();
await IdentitySeeder.SeedRolesAsync(app.Services);

app.UseAuthentication();
app.UseAuthorization();

// app.MapGroup("/api/auth")
//     .MapIdentityApi<ApplicationUser>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
