using OpenTelemetry;
using OpenTelemetry.Exporter;
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

const string serviceName = "callingService";

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(serviceName))
            .AddAspNetCoreInstrumentation()  // Automatically trace incoming HTTP requests to ASP.NET Core
            .AddHttpClientInstrumentation()  // Automatically trace outgoing HTTP requests from HttpClient
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri(otlpEndpoint);  // Set OTLP endpoint (by default Jaeger's gRPC endpoint)
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;  // gRPC is the preferred protocol
            })
            .SetSampler(new AlwaysOnSampler());  // Always sample for testing purposes
    });


//builder.Services.AddOpenTelemetry()
//      .ConfigureResource(resource => resource.AddService(serviceName))
//      .WithTracing(tracing => tracing
//          .AddAspNetCoreInstrumentation().AddOtlpExporter(
//          options =>
//            {
//                options.Endpoint = new Uri(otlpEndpoint);
//                options.Protocol = OtlpExportProtocol.Grpc;
//            }
//          ))
//      .WithMetrics(metrics => metrics
//          .AddAspNetCoreInstrumentation().AddOtlpExporter(
//          options =>
//          {
//              options.Endpoint = new Uri(otlpEndpoint);
//              options.Protocol = OtlpExportProtocol.Grpc;
//          }
//          ));



var app = builder.Build();


app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();


app.MapGet("/helloworld", () =>
{
    return otlpEndpoint;
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
