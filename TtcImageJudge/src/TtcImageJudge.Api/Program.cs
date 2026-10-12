using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.Json;
using TtcImageJudge.Api;
using TtcImageJudge.Core;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase)));
builder.Services.AddSingleton<JudgeDatabase>();
builder.Services.AddHttpClient("executor", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Executor:Url"] ?? "http://127.0.0.1:5073");
    client.Timeout = TimeSpan.FromSeconds(35);
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration["Web:Origin"] ?? "http://127.0.0.1:5071")
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
var wireJson = new JsonSerializerOptions(JsonSerializerDefaults.Web);
wireJson.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
app.UseCors();
var db = app.Services.GetRequiredService<JudgeDatabase>();
await db.InitializeAsync();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", database = db.DatabasePath }));
app.MapGet("/api/exercises", async () => Results.Ok(await db.ListExercisesAsync()));
app.MapGet("/api/exercises/{id:guid}", async (Guid id) =>
    await db.GetExerciseAsync(id) is { } exercise ? Results.Ok(PublicView(exercise)) : Results.NotFound());
app.MapGet("/api/submissions", async (Guid? exerciseId) =>
    Results.Ok(await db.ListSubmissionsAsync(exerciseId)));
app.MapGet("/api/submissions/{id:guid}", async (Guid id) =>
    await db.GetSubmissionAsync(id) is { } submission ? Results.Ok(submission) : Results.NotFound());

app.MapPost("/api/submissions", async (SubmissionRequest request, IHttpClientFactory clients) =>
{
    var exercise = await db.GetExerciseAsync(request.ExerciseId);
    if (exercise is null) return Results.NotFound(new { message = "Exercício não encontrado." });
    if (string.IsNullOrWhiteSpace(request.SourceCode) || request.SourceCode.Length > 100_000)
        return Results.BadRequest(new { message = "O código deve ter entre 1 e 100.000 caracteres." });

    var id = Guid.NewGuid();
    ExecuteResponse result;
    try
    {
        var response = await clients.CreateClient("executor")
            .PostAsJsonAsync("/internal/execute", new ExecuteRequest(id, exercise, request.SourceCode));
        result = response.IsSuccessStatusCode
            ? (await response.Content.ReadFromJsonAsync<ExecuteResponse>(wireJson)
               ?? Infrastructure("Resposta vazia do executor."))
            : Infrastructure($"Executor indisponível (HTTP {(int)response.StatusCode}).");
    }
    catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
    {
        result = Infrastructure("Não foi possível contatar o executor isolado.");
    }

    var submission = new SubmissionView(id, exercise.Id, DateTimeOffset.UtcNow, result.Verdict,
        result.Message, result.CompilerOutput, exercise.Version, exercise.ReferenceVersion,
        exercise.RulesVersion, request.SourceCode, result.Tests);
    await db.SaveExpectedArtifactsAsync(exercise, result.Tests);
    await db.SaveSubmissionAsync(submission);
    return Results.Ok(submission);
});

app.MapPut("/api/admin/exercises/{id:guid}", async (Guid id, ExerciseDetails input, HttpContext context) =>
{
    if (!IsAdmin(context, app.Configuration)) return Results.Unauthorized();
    if (id != input.Id) return Results.BadRequest(new { message = "IDs divergentes." });
    var existing = await db.GetExerciseAsync(id);
    if (existing is not null)
    {
        var tests = input.Tests.Select(test => test.ReferenceCode is not null ? test : test with
        { ReferenceCode = existing.Tests.FirstOrDefault(old => old.Id == test.Id)?.ReferenceCode }).ToArray();
        input = input with { Tests = tests };
        var rulesChanged = System.Text.Json.JsonSerializer.Serialize(existing.Rules) !=
                           System.Text.Json.JsonSerializer.Serialize(input.Rules);
        var referenceChanged = System.Text.Json.JsonSerializer.Serialize(existing.Tests) !=
                               System.Text.Json.JsonSerializer.Serialize(input.Tests);
        input = input with
        {
            Version = Math.Max(input.Version, existing.Version + 1),
            ReferenceVersion = referenceChanged ? existing.ReferenceVersion + 1 : existing.ReferenceVersion,
            RulesVersion = rulesChanged ? existing.RulesVersion + 1 : existing.RulesVersion
        };
    }
    await db.UpsertExerciseAsync(input, invalidateGeneratedAnswers: true);
    return Results.NoContent();
});
app.MapGet("/api/admin/exercises/{id:guid}", async (Guid id, HttpContext context) =>
{
    if (!IsAdmin(context, app.Configuration)) return Results.Unauthorized();
    return await db.GetExerciseAsync(id) is { } exercise ? Results.Ok(exercise) : Results.NotFound();
});

app.Run();

static ExecuteResponse Infrastructure(string message) => new(Verdict.InfrastructureFailure, message, "", false,
    "A análise de requisito não foi executada.", []);

static ExerciseDetails PublicView(ExerciseDetails exercise) => exercise with
{
    Tests = exercise.Tests.Select(test => test with
    { InputImageBase64 = null, ExpectedImageBase64 = null, ReferenceCode = null }).ToArray()
};

static bool IsAdmin(HttpContext context, IConfiguration configuration)
{
    var remote = context.Connection.RemoteIpAddress;
    if (remote is not null && !IPAddress.IsLoopback(remote)) return false;
    var expected = configuration["Admin:Key"];
    return !string.IsNullOrWhiteSpace(expected) &&
           context.Request.Headers.TryGetValue("X-Admin-Key", out var supplied) &&
           CryptographicOperations.FixedTimeEquals(System.Text.Encoding.UTF8.GetBytes(expected),
               System.Text.Encoding.UTF8.GetBytes(supplied.ToString()));
}

public partial class Program;
