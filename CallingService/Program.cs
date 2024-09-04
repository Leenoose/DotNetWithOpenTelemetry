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
      .ConfigureResource(resource => resource.AddService("service-caller"))
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
                .AddService("service-caller"))
        .AddConsoleExporter();
});



var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


app.MapGet("/helloworld", () =>
{
    return "Test";
})
.WithName("HelloWorld")
.WithOpenApi();

app.MapGet("/fetch_data", async (string url) =>
{
    using var httpClient = new HttpClient();


    HttpResponseMessage response = await httpClient.GetAsync(url);

    response.EnsureSuccessStatusCode();

    string responseData = await response.Content.ReadAsStringAsync();

    return Results.Ok(responseData);
})
.WithName("FetchData")
.WithOpenApi();

app.Run();

internal record Test()
{
    public int test = 0;
}
