using Npgsql;
using MySql.Data.MySqlClient;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;



var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddOpenTelemetry()
      .ConfigureResource(resource => resource.AddService("database-caller"))
      .WithTracing(tracing => tracing
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter())
      .WithMetrics(metrics => metrics
          .AddAspNetCoreInstrumentation()
          .AddConsoleExporter());

builder.Logging.AddOpenTelemetry(options =>
{
    options
        .SetResourceBuilder(
            ResourceBuilder.CreateDefault()
                .AddService("database-caller"))
        .AddConsoleExporter();
});





var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();


app.MapGet("/postgresql", async () =>
{
    string connectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Connection string not found in environment variables.");

    await using var conn = new NpgsqlConnection(connectionString);
    await conn.OpenAsync();

    using (var cmd = new NpgsqlCommand("SELECT 'Hello, PostgreSQL!'", conn))
    {
        var result = await cmd.ExecuteScalarAsync();
        return Results.Ok(result?.ToString());
    }

})
.WithOpenApi();

app.MapGet("/mysql", async () =>
{
    string connectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
        ?? throw new InvalidOperationException("Connection string not found in environment variables.");

    await using var conn = new MySqlConnection(connectionString);
    await conn.OpenAsync();

    using (var cmd = new MySqlCommand("SELECT 'Hello, MySQL!'", conn))
    {
        var result = await cmd.ExecuteScalarAsync();
        return Results.Ok(result?.ToString());
    }
})
.WithOpenApi();

app.Run();
