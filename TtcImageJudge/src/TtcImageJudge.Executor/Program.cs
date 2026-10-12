using System.Net;
using System.Text.Json.Serialization;
using TtcImageJudge.Core;
using TtcImageJudge.Executor;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5073");
builder.Services.AddSingleton<DirectMemoryAnalyzer>();
builder.Services.AddSingleton<ContainerExecutionEngine>();
var app = builder.Build();

app.Use(async (context, next) =>
{
    var remote = context.Connection.RemoteIpAddress;
    if (remote is not null && !IPAddress.IsLoopback(remote))
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return;
    }
    await next();
});

app.MapGet("/health", (ContainerExecutionEngine engine) => Results.Ok(engine.GetStatus()));
app.MapPost("/internal/execute", async (ExecuteRequest request, ContainerExecutionEngine engine,
    DirectMemoryAnalyzer analyzer, CancellationToken cancellationToken) =>
{
    var analysis = analyzer.Analyze(request.SourceCode);
    var response = await engine.ExecuteAsync(request, analysis, cancellationToken);
    return Results.Ok(response);
});

app.Run();
