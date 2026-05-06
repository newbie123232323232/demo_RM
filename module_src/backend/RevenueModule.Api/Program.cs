using Microsoft.EntityFrameworkCore;
using RevenueModule.Api.Data;
using RevenueModule.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = (builder.Configuration["CORS_ALLOWED_ORIGINS"] ?? "http://localhost:4200,http://127.0.0.1:4200")
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        policy
            .WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var revenueDb = ResolveRevenueDbConnectionString(builder.Configuration);
builder.Services.AddDbContext<RevenueDbContext>(options => options.UseNpgsql(revenueDb));
builder.Services.AddScoped<BillPreviewCalculator>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Profile `http` chỉ listen :5093 — tránh redirect sang HTTPS gây khó hiểu khi mở trình duyệt
// app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapGet("/", () => Results.Text(
    "RevenueModule.Api OK. GET /health (JSON) hoặc /WeatherForecast (mẫu template).",
    "text/plain; charset=utf-8"));

app.MapGet("/health", async (RevenueDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return Results.Json(new
            {
                status = "degraded",
                service = "RevenueModule.Api",
                connectionStringConfigured = !string.IsNullOrWhiteSpace(revenueDb),
                database = "unreachable",
            }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Results.Json(new
        {
            status = "ok",
            service = "RevenueModule.Api",
            connectionStringConfigured = !string.IsNullOrWhiteSpace(revenueDb),
            database = "connected",
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new
        {
            status = "degraded",
            service = "RevenueModule.Api",
            connectionStringConfigured = !string.IsNullOrWhiteSpace(revenueDb),
            database = "error",
            error = ex.Message,
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapControllers();

app.Run();

static string ResolveRevenueDbConnectionString(ConfigurationManager configuration)
{
    var fromConnectionStrings = configuration.GetConnectionString("RevenueDb");
    if (!string.IsNullOrWhiteSpace(fromConnectionStrings))
    {
        return fromConnectionStrings;
    }

    var databaseUrl = configuration["DATABASE_URL"];
    if (string.IsNullOrWhiteSpace(databaseUrl))
    {
        throw new InvalidOperationException(
            "Missing database config. Set ConnectionStrings:RevenueDb or DATABASE_URL environment variable.");
    }

    return ConvertPostgresUrlToNpgsql(databaseUrl);
}

static string ConvertPostgresUrlToNpgsql(string rawUrl)
{
    // Accept formats like:
    // - postgresql://user:pass@host:5432/db
    // - postgresql+asyncpg://user:pass@host:5432/db
    var normalized = rawUrl.Replace("postgresql+asyncpg://", "postgresql://", StringComparison.OrdinalIgnoreCase);
    if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
    {
        throw new InvalidOperationException("DATABASE_URL is not a valid postgres URI.");
    }

    var userInfo = uri.UserInfo.Split(':', 2);
    if (userInfo.Length != 2)
    {
        throw new InvalidOperationException("DATABASE_URL must include username and password.");
    }

    var database = uri.AbsolutePath.Trim('/');
    if (string.IsNullOrWhiteSpace(database))
    {
        throw new InvalidOperationException("DATABASE_URL must include database name.");
    }

    var port = uri.Port > 0 ? uri.Port : 5432;
    return $"Host={uri.Host};Port={port};Database={database};Username={userInfo[0]};Password={userInfo[1]}";
}

public partial class Program;
