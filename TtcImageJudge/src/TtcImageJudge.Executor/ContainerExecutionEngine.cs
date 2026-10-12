using System.Diagnostics;
using System.Text.Json;
using TtcImageJudge.Core;

namespace TtcImageJudge.Executor;

public sealed class ContainerExecutionEngine(IConfiguration configuration)
{
    private readonly string _mode = configuration["Isolation:Mode"] ?? "Blocked";
    private readonly string _image = configuration["Isolation:Image"] ?? "ttc-image-judge-runner:net10-windows";
    private readonly string _tasksRoot = Path.GetFullPath(configuration["Isolation:TasksRoot"] ??
        Path.Combine(Path.GetTempPath(), "ttc-image-judge"));

    public object GetStatus() => new
    {
        status = _mode.Equals("DockerWindows", StringComparison.OrdinalIgnoreCase) ? "configured" : "blocked",
        mode = _mode,
        platform = Environment.OSVersion.Platform.ToString(),
        message = _mode.Equals("DockerWindows", StringComparison.OrdinalIgnoreCase)
            ? "Execução requer Docker em modo Windows e a imagem configurada."
            : "Execução bloqueada: configure Isolation:Mode=DockerWindows; não há fallback irrestrito."
    };

    public async Task<ExecuteResponse> ExecuteAsync(ExecuteRequest request, RequirementAnalysis analysis,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindows() || !_mode.Equals("DockerWindows", StringComparison.OrdinalIgnoreCase))
            return Infrastructure("Executor bloqueado: isolamento por contêiner Windows não está configurado.", analysis);

        Directory.CreateDirectory(_tasksRoot);
        var taskDirectory = Path.Combine(_tasksRoot, request.SubmissionId.ToString("N"));
        Directory.CreateDirectory(taskDirectory);
        var containerName = "ttc-judge-" + request.SubmissionId.ToString("N");
        try
        {
            await File.WriteAllTextAsync(Path.Combine(taskDirectory, "Task.csproj"), RunnerSource.Project,
                cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(taskDirectory, "Program.cs"),
                RunnerSource.Build(request), cancellationToken);

            var mount = $"type=bind,source={taskDirectory},target=C:\\task";
            var compileArguments = new[]
            {
                "run", "--rm", "--name", containerName + "-compile", "--network", "none", "--memory", "768m",
                "--cpus", "1", "--pids-limit", "64", "--mount", mount, "--workdir", "C:\\task", _image,
                "cmd.exe", "/d", "/s", "/c", "dotnet restore Task.csproj --ignore-failed-sources && dotnet build Task.csproj --no-restore -c Release"
            };
            var compileResult = await RunAsync("docker", compileArguments, TimeSpan.FromSeconds(45), cancellationToken);
            if (compileResult.TimedOut)
            {
                await RunAsync("docker", ["rm", "-f", containerName + "-compile"], TimeSpan.FromSeconds(10), CancellationToken.None);
                return Infrastructure("A infraestrutura excedeu o tempo de compilação; a solução não foi julgada.", analysis,
                    compileResult.Output);
            }
            if (compileResult.ExitCode != 0)
            {
                var compilationError = compileResult.Output.Contains("error CS", StringComparison.OrdinalIgnoreCase);
                return compilationError
                    ? new ExecuteResponse(Verdict.CompilationError, "O código não compilou.", compileResult.Output,
                        analysis.Met, analysis.Message, [])
                    : Infrastructure("Falha ao preparar o contêiner de compilação.", analysis, compileResult.Output);
            }
            if (request.Exercise.Rules.RequiresDirectMemoryAccess && !analysis.Met)
                return new ExecuteResponse(Verdict.RequirementNotMet,
                    "O código compilou, mas o requisito estrutural de acesso direto não foi atendido.",
                    compileResult.Output, false, analysis.Message, []);

            var runArguments = new[]
            {
                "run", "--rm", "--name", containerName, "--network", "none", "--memory", $"{request.Exercise.Rules.MemoryLimitMb}m",
                "--cpus", "1", "--pids-limit", "64", "--mount", mount, "--workdir", "C:\\task", _image,
                "dotnet", "C:\\task\\bin\\Release\\net10.0-windows\\Task.dll"
            };
            var processResult = await RunAsync("docker", runArguments,
                TimeSpan.FromMilliseconds(request.Exercise.Rules.TimeLimitMs), cancellationToken);
            if (processResult.TimedOut)
            {
                await RunAsync("docker", ["rm", "-f", containerName], TimeSpan.FromSeconds(10), CancellationToken.None);
                return new ExecuteResponse(Verdict.TimeLimitExceeded, "A árvore do contêiner foi encerrada pelo limite de tempo.",
                    processResult.Output, analysis.Met, analysis.Message, []);
            }
            var resultFile = Path.Combine(taskDirectory, "result.json");
            if (!File.Exists(resultFile))
            {
                if (processResult.Output.Contains("out of memory", StringComparison.OrdinalIgnoreCase) || processResult.ExitCode == 137)
                    return new ExecuteResponse(Verdict.MemoryLimitExceeded, "O contêiner excedeu o limite de memória.", processResult.Output,
                        analysis.Met, analysis.Message, []);
                return Infrastructure("O contêiner terminou sem produzir resultado; verifique a imagem e o gabarito.", analysis,
                    processResult.Output);
            }
            var response = JsonSerializer.Deserialize<ExecuteResponse>(await File.ReadAllTextAsync(resultFile, cancellationToken),
                new JsonSerializerOptions(JsonSerializerDefaults.Web));
            return response ?? Infrastructure("Resultado inválido produzido pelo executor.", analysis);
        }
        catch (Exception exception) when (exception is IOException or System.ComponentModel.Win32Exception)
        {
            return Infrastructure("Não foi possível iniciar o Docker Windows: " + exception.Message, analysis);
        }
        finally
        {
            try { if (Directory.Exists(taskDirectory)) Directory.Delete(taskDirectory, true); } catch { /* limpeza posterior */ }
        }
    }

    private static ExecuteResponse Infrastructure(string message, RequirementAnalysis analysis, string output = "") =>
        new(Verdict.InfrastructureFailure, message, output, analysis.Met, analysis.Message, []);

    private static async Task<ProcessResult> RunAsync(string file, IEnumerable<string> arguments, TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo(file)
        { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true } };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        try { await process.WaitForExitAsync(timeoutCts.Token); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { try { process.Kill(true); } catch { } return new(-1, await outputTask + await errorTask, true); }
        return new(process.ExitCode, await outputTask + await errorTask, false);
    }

    private sealed record ProcessResult(int ExitCode, string Output, bool TimedOut);
}
