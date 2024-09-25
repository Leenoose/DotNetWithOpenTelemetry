
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);

string postgresConnectionString = Environment.GetEnvironmentVariable("POSTGRESQL_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string not found in environment variables.");

string mySqlConnectionString = Environment.GetEnvironmentVariable("MYSQL_CONNECTION_STRING")
    ?? throw new InvalidOperationException("Connection string not found in environment variables.");



// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddDbContext<PostgresDbContext>(options => options.UseNpgsql(postgresConnectionString));
builder.Services.AddDbContext<MySqlDbContext>(options => options.UseMySQL(mySqlConnectionString));


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

using (var scope = app.Services.CreateScope())
{
    var dbContexts = scope.ServiceProvider.GetServices<DbContext>();
    foreach (var context in dbContexts)
    {
        context.Database.Migrate();
    }
}
app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();


app.MapGet("/postgresql", async (PostgresDbContext dbContext) =>
{
    dbContext.test.Add(new Test { Message = "Hello, PostgreSQL!" });
    await dbContext.SaveChangesAsync();

    // Fetch data from the database
    var greeting = await dbContext.test.FirstOrDefaultAsync();
    return Results.Ok(greeting?.Message);

})
.WithOpenApi();

app.MapGet("/mysql", async (MySqlDbContext dbContext) =>
{
    dbContext.test.Add(new Test { Message = "Hello, MySQL!" });
    await dbContext.SaveChangesAsync();

    // Fetch data from the database
    var greeting = await dbContext.test.FirstOrDefaultAsync();
    return Results.Ok(greeting?.Message);
})
.WithOpenApi();

app.Run();

public class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    public DbSet<Test> test { get; set; }
}

public class MySqlDbContext : DbContext {
    public MySqlDbContext(DbContextOptions<MySqlDbContext> options) : base(options)
    {
    }
    public DbSet<Test> test { get; set; }

}

public class Test
{
    public int Id { get; set; }
    public string Message { get; set; }
}
