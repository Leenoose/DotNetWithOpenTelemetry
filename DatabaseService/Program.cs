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

string otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT") ?? "http://localhost:4317";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("DatabaseService"))
            .AddAspNetCoreInstrumentation()  // Automatically trace incoming HTTP requests to ASP.NET Core
            .AddHttpClientInstrumentation()  // Automatically trace outgoing HTTP requests from HttpClient
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);  // Use the OTLP endpoint from environment variables
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;  // Default protocol
            })
            .SetSampler(new AlwaysOnSampler());  // Adjust the sampling rate if necessary (AlwaysOnSampler sends all traces)
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
