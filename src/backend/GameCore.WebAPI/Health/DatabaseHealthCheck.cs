using System.Text.Json;
using GameCore.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GameCore.WebAPI.Health;

/// <summary>
/// Reports unhealthy when GameCoreDB cannot be reached (SQL Server down, database
/// missing or credentials rejected).
/// </summary>
public sealed class DatabaseHealthCheck(GameCoreDbContext db) : IHealthCheck
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(Timeout);
        try
        {
            return await db.Database.CanConnectAsync(timeout.Token)
                ? HealthCheckResult.Healthy("GameCoreDB is reachable.")
                : HealthCheckResult.Unhealthy("GameCoreDB is not reachable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"GameCoreDB did not respond within {Timeout.TotalSeconds:0} seconds.");
        }
    }
}

public static class HealthResponseWriter
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new
        {
            status = report.Status.ToString(),
            utc = DateTime.UtcNow,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description
            })
        };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, Json));
    }
}
