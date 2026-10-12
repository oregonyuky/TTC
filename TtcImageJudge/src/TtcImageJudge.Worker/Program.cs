using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

return args.Length switch
{
    4 when args[0] == "compile" => Compile(args[1], args[2], args[3]),
    2 when args[0] == "run" => await Run(args[1]),
    _ => Usage()
};

static int Compile(string sourcePath, string assemblyPath, string logPath)
{
    try
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(sourcePath),
            new CSharpParseOptions(LanguageVersion.Latest), sourcePath);
        var referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trustedAssemblies = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
        if (!string.IsNullOrWhiteSpace(trustedAssemblies))
            foreach (var path in trustedAssemblies.Split(Path.PathSeparator)) referencePaths.Add(path);
        foreach (var path in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
        {
            try { _ = AssemblyName.GetAssemblyName(path); referencePaths.Add(path); }
            catch (BadImageFormatException) { }
            catch (FileLoadException) { }
        }

        var compilation = CSharpCompilation.Create("Task", [syntaxTree],
            referencePaths.Select(path => MetadataReference.CreateFromFile(path)),
            new CSharpCompilationOptions(OutputKind.ConsoleApplication,
                optimizationLevel: OptimizationLevel.Release, allowUnsafe: true,
                deterministic: true, platform: Platform.X64));
        var emit = compilation.Emit(assemblyPath);
        var diagnostics = string.Join(Environment.NewLine,
            emit.Diagnostics.OrderBy(item => item.Location.SourceSpan.Start).Select(item => item.ToString()));
        File.WriteAllText(logPath, diagnostics);
        return emit.Success ? 0 : 1;
    }
    catch (Exception exception)
    {
        File.WriteAllText(logPath, "Falha interna do compilador: " + exception);
        return 70;
    }
}

static async Task<int> Run(string assemblyPath)
{
    try
    {
        var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
        var entryPoint = assembly.EntryPoint ?? throw new InvalidOperationException("Assembly sem ponto de entrada.");
        var parameters = entryPoint.GetParameters().Length == 0 ? null : new object?[] { Array.Empty<string>() };
        var value = entryPoint.Invoke(null, parameters);
        if (value is Task task)
        {
            await task;
            value = task.GetType().IsGenericType ? task.GetType().GetProperty("Result")?.GetValue(task) : null;
        }
        return value is int exitCode ? exitCode : 0;
    }
    catch (TargetInvocationException exception) when (exception.InnerException is not null)
    {
        Console.Error.WriteLine(exception.InnerException);
        return 3;
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine(exception);
        return 3;
    }
}

static int Usage()
{
    Console.Error.WriteLine("Uso: worker compile <fonte> <assembly> <log> | worker run <assembly>");
    return 64;
}
