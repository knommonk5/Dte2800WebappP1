using Bruktborsen.Api.Data;
using Bruktborsen.Api.Data.Seed;
using Bruktborsen.Api.Models;
using Bruktborsen.Api.Repositories;
using Bruktborsen.Api.Repositories.Interfaces;
using Bruktborsen.Api.Services;
using Bruktborsen.Api.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Bruktborsen.Api.Services.Identity;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration
    .GetConnectionString("BruktborsenDatabase")
    ?? "Data Source=bruktborsen.db";

builder.Services.AddDbContext<BruktborsenDbContext>(options =>
    options.UseSqlite(connectionString));

// Identity
builder.Services
    .AddIdentityApiEndpoints<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<BruktborsenDbContext>()
    .AddUserManager<ApplicationUserManager>();

builder.Services.AddHttpContextAccessor();

// Controllers og OpenAPI
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS for Blazor-klienten
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("BlazorClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Repositories
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IListingRepository, ListingRepository>();
builder.Services.AddScoped<IFavoriteRepository, FavoriteRepository>();
builder.Services.AddScoped<IOfferRepository, OfferRepository>();
builder.Services.AddScoped<IPriceChangeRepository, PriceChangeRepository>();
builder.Services.AddScoped<IAnalyticsRepository, AnalyticsRepository>();

// Services
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IListingService, ListingService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IOfferService, OfferService>();
builder.Services.AddScoped<IPriceChangeService, PriceChangeService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

var app = builder.Build();

// Opprett nødvendige Identity-roller
await IdentitySeeder.SeedRolesAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("BlazorClient");

app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) &&
        string.Equals(
            context.Request.Path.Value?.TrimEnd('/'),
            "/api/auth/register",
            StringComparison.OrdinalIgnoreCase))
    {
        context.Request.EnableBuffering();
    }

    await next(context);
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGroup("/api/auth").MapIdentityApi<ApplicationUser>();

app.MapControllers();

app.Run();

// Gjør Program tilgjengelig for integrasjonstester.
public partial class Program { }