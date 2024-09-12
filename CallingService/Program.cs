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
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("CallingService"))
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
