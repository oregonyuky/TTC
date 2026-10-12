using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TtcImageJudge.Executor;

public sealed record RequirementAnalysis(bool Met, string Message);

public sealed class DirectMemoryAnalyzer
{
    public RequirementAnalysis Analyze(string source)
    {
        var lines = source.Replace("\r", "").Split('\n');
        var usings = string.Join('\n', lines.Where(line => line.TrimStart().StartsWith("using ", StringComparison.Ordinal)));
        var body = string.Join('\n', lines.Where(line => !line.TrimStart().StartsWith("using ", StringComparison.Ordinal)));
        var tree = CSharpSyntaxTree.ParseText($"{usings}\npublic static class Submission {{\n{body}\n}}",
            new CSharpParseOptions(LanguageVersion.Latest));
        var root = tree.GetRoot();
        var method = root.DescendantNodes().OfType<MethodDeclarationSyntax>()
            .FirstOrDefault(node => node.Identifier.ValueText == "Blur5x5");
        if (method is null)
            return new(false, "A função Blur5x5 não foi encontrada na árvore sintática.");

        var hasUnsafe = method.Modifiers.Any(token => token.IsKind(SyntaxKind.UnsafeKeyword));
        var hasPointer = method.DescendantNodes().OfType<PointerTypeSyntax>().Any();
        var invocations = method.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .Select(node => node.Expression.ToString()).ToArray();
        var members = method.DescendantNodes().OfType<MemberAccessExpressionSyntax>()
            .Select(node => node.Name.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
        var hasLockPair = invocations.Any(name => name.EndsWith(".LockBits", StringComparison.Ordinal)) &&
                          invocations.Any(name => name.EndsWith(".UnlockBits", StringComparison.Ordinal));
        var hasAddressing = members.Contains("Scan0") && members.Contains("Stride");
        var met = hasUnsafe && hasPointer && hasLockPair && hasAddressing;
        var details = $"unsafe={hasUnsafe}; ponteiro tipado={hasPointer}; LockBits/UnlockBits={hasLockPair}; Scan0/Stride={hasAddressing}.";
        return new(met, details + " A análise estrutural reduz falsos positivos, mas não prova que cada pixel foi acessado corretamente; isso é avaliado separadamente pelos testes de imagem.");
    }
}
