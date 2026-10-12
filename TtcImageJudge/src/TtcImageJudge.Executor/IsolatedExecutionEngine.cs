using System.Text.Json;
using TtcImageJudge.Core;

namespace TtcImageJudge.Executor;

public sealed class IsolatedExecutionEngine(IConfiguration configuration, IWebHostEnvironment environment)
{
    private const string WorkerExecutable = "TtcImageJudge.Worker.exe";
    private readonly string _mode = configuration["Isolation:Mode"] ?? "AppContainer";
    private readonly string _tasksRoot = Path.GetFullPath(Environment.ExpandEnvironmentVariables(
        configuration["Isolation:TasksRoot"] ?? Path.Combine(Path.GetTempPath(), "ttc-image-judge")));
    private readonly string? _configuredWorkerPath = configuration["Isolation:WorkerPath"];
    private readonly string _contentRoot = environment.ContentRootPath;

    public object GetStatus()
    {
        var worker = FindWorkerDirectory();
        var supported = OperatingSystem.IsWindows() && AppContainerSandbox.NativeApisAvailable && worker is not null;
        return new
        {
            status = _mode.Equals("AppContainer", StringComparison.OrdinalIgnoreCase) && supported
                ? "configured" : "blocked",
            mode = _mode,
            platform = Environment.OSVersion.VersionString,
            isolation = "AppContainer + Job Object",
            worker = worker is null ? "ausente" : "autocontido",
            message = supported
                ? "Sandbox nativo disponível; o compilador e a solução executam em um AppContainer sem rede."
                : "AppContainer ou worker autocontido indisponível; a execução permanece bloqueada."
        };
    }

    public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request, RequirementAnalysis analysis,
        CancellationToken cancellationToken)
    {
        if (!_mode.Equals("AppContainer", StringComparison.OrdinalIgnoreCase) ||
            !OperatingSystem.IsWindows() || !AppContainerSandbox.NativeApisAvailable)
            return Infrastructure("Executor bloqueado: AppContainer não está disponível ou não foi habilitado.", analysis);

        var workerSource = FindWorkerDirectory();
        if (workerSource is null)
            return Infrastructure("Executor bloqueado: compile o worker autocontido antes de aceitar submissões.", analysis);

        Directory.CreateDirectory(_tasksRoot);
        var taskDirectory = Path.Combine(_tasksRoot, request.SubmissionId.ToString("N"));
        Directory.CreateDirectory(taskDirectory);
        AppContainerSandbox? sandbox = null;
        try
        {
            var sourcePath = Path.Combine(taskDirectory, "Program.cs");
            await File.WriteAllTextAsync(sourcePath, RunnerSource.Build(request), cancellationToken);
            var workerDirectory = Path.Combine(taskDirectory, ".worker");
            CopyDirectory(workerSource, workerDirectory);
            var worker = Path.Combine(workerDirectory, WorkerExecutable);

            sandbox = AppContainerSandbox.Create("TtcImageJudge." + request.SubmissionId.ToString("N"), taskDirectory);
            var assembly = Path.Combine(taskDirectory, "Task.dll");
            var compileLog = Path.Combine(taskDirectory, "compile.log");
            var compile = await sandbox.RunProcessAsync(worker,
                ["compile", sourcePath, assembly, compileLog],
                TimeSpan.FromSeconds(45), 768, 4, cancellationToken);
            var compilerOutput = File.Exists(compileLog)
                ? await File.ReadAllTextAsync(compileLog, cancellationToken)
                : compile.Diagnostic;

            if (compile.TimedOut)
                return Infrastructure("A compilação isolada excedeu 45 segundos; a solução não foi julgada.", analysis,
                    compilerOutput);
            if (compile.MemoryLimitExceeded)
                return Infrastructure("A infraestrutura de compilação atingiu seu limite de memória.", analysis,
                    compilerOutput);
            if (compile.ExitCode != 0)
            {
                var isCodeError = compile.ExitCode == 1 &&
                                  compilerOutput.Contains("error CS", StringComparison.OrdinalIgnoreCase);
                return isCodeError
                    ? new ExecuteResponse(Verdict.CompilationError, "O código não compilou.", compilerOutput,
                        analysis.Met, analysis.Message, [])
                    : Infrastructure("Falha no compilador dentro do AppContainer.", analysis, compilerOutput);
            }
            if (request.Exercise.Rules.RequiresDirectMemoryAccess && !analysis.Met)
                return new ExecuteResponse(Verdict.RequirementNotMet,
                    "O código compilou, mas o requisito estrutural de acesso direto não foi atendido.",
                    compilerOutput, false, analysis.Message, []);

            var run = await sandbox.RunProcessAsync(worker, ["run", assembly],
                TimeSpan.FromMilliseconds(request.Exercise.Rules.TimeLimitMs),
                request.Exercise.Rules.MemoryLimitMb, 4, cancellationToken);
            if (run.TimedOut)
                return new ExecuteResponse(Verdict.TimeLimitExceeded,
                    "Toda a árvore de processos do AppContainer foi encerrada pelo limite de tempo.",
                    compilerOutput, analysis.Met, analysis.Message, []);
            if (run.MemoryLimitExceeded)
                return new ExecuteResponse(Verdict.MemoryLimitExceeded,
                    "O Job Object encerrou ou bloqueou o processo no limite de memória.",
                    compilerOutput, analysis.Met, analysis.Message, []);

            var resultFile = Path.Combine(taskDirectory, "result.json");
            if (!File.Exists(resultFile))
                return run.ExitCode == 0
                    ? Infrastructure("O processo terminou sem produzir um resultado válido.", analysis, run.Diagnostic)
                    : new ExecuteResponse(Verdict.RuntimeError,
                        $"O processo terminou com o código 0x{run.ExitCode:X8}.", compilerOutput,
                        analysis.Met, analysis.Message, []);

            var response = JsonSerializer.Deserialize<ExecuteResponse>(
                await File.ReadAllTextAsync(resultFile, cancellationToken),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return response is null
                ? Infrastructure("Resultado inválido produzido pelo processo isolado.", analysis)
                : response with
                {
                    CompilerOutput = compilerOutput,
                    RequirementAnalysis = analysis.Message,
                    DirectMemoryRequirementMet = analysis.Met
                };
        }
        catch (SandboxException exception)
        {
            return Infrastructure("Não foi possível estabelecer o AppContainer: " + exception.Message, analysis);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
        {
            return Infrastructure("Falha ao preparar a tarefa isolada: " + exception.Message, analysis);
        }
        finally
        {
            sandbox?.Dispose();
            try { if (Directory.Exists(taskDirectory)) Directory.Delete(taskDirectory, true); } catch { }
        }
    }

    private string? FindWorkerDirectory()
    {
        var candidates = new List<string>();
        if (!string.IsNullOrWhiteSpace(_configuredWorkerPath))
        {
            var expanded = Environment.ExpandEnvironmentVariables(_configuredWorkerPath);
            candidates.Add(Path.IsPathRooted(expanded) ? expanded : Path.Combine(_contentRoot, expanded));
        }
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "worker"));
        foreach (var buildConfiguration in new[] { "Debug", "Release" })
            candidates.Add(Path.GetFullPath(Path.Combine(_contentRoot, "..", "TtcImageJudge.Worker", "bin",
                buildConfiguration, "net10.0-windows", "win-x64")));
        return candidates.FirstOrDefault(path => File.Exists(Path.Combine(path, WorkerExecutable)));
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (var directory in Directory.EnumerateDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }

    private static ExecuteResponse Infrastructure(string message, RequirementAnalysis analysis, string output = "") =>
        new(Verdict.InfrastructureFailure, message, output, analysis.Met, analysis.Message, []);
}
