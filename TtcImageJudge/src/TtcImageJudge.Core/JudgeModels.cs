namespace TtcImageJudge.Core;

public enum Verdict
{
    Accepted, WrongAnswer, CompilationError, RuntimeError, TimeLimitExceeded,
    MemoryLimitExceeded, RequirementNotMet, InfrastructureFailure
}

public enum ExpectedOutputMode { UploadedImage, ReferenceImplementation }

public sealed record ExerciseRules(
    string FunctionSignature,
    string InputFormat,
    string BorderHandling,
    string RoundingAndSaturation,
    bool ExactComparison,
    int ChannelTolerance,
    int TimeLimitMs,
    int MemoryLimitMb,
    bool RequiresDirectMemoryAccess);

public sealed record ExerciseSummary(Guid Id, string Slug, string Title, int Version, string Difficulty);

public sealed record ExerciseDetails(
    Guid Id,
    string Slug,
    string Title,
    string Statement,
    string Notes,
    string Difficulty,
    int Version,
    int ReferenceVersion,
    int RulesVersion,
    ExerciseRules Rules,
    string StarterCode,
    IReadOnlyList<TestCaseDefinition> Tests);

public sealed record TestCaseDefinition(
    Guid Id,
    string Name,
    string Pattern,
    int Width,
    int Height,
    int Seed,
    ExpectedOutputMode ExpectedOutputMode,
    string? InputImageBase64 = null,
    string? ExpectedImageBase64 = null,
    string? ReferenceCode = null,
    int GeneratedForReferenceVersion = 0,
    int GeneratedForRulesVersion = 0);

public sealed record SubmissionRequest(Guid ExerciseId, string SourceCode);

public sealed record ExecuteRequest(
    Guid SubmissionId,
    ExerciseDetails Exercise,
    string SourceCode,
    bool IsReference = false);

public sealed record PixelDifference(
    int DifferentPixels,
    int TotalPixels,
    double Percentage,
    int? FirstX,
    int? FirstY,
    int MaxChannelDelta,
    string? DifferenceImageBase64 = null);

public sealed record TestResult(
    string TestName,
    Verdict Verdict,
    string Message,
    long ElapsedMs,
    PixelDifference? Difference = null,
    string? InputImageBase64 = null,
    string? ExpectedImageBase64 = null,
    string? ActualImageBase64 = null);

public sealed record ExecuteResponse(
    Verdict Verdict,
    string Message,
    string CompilerOutput,
    bool DirectMemoryRequirementMet,
    string RequirementAnalysis,
    IReadOnlyList<TestResult> Tests);

public sealed record SubmissionView(
    Guid Id,
    Guid ExerciseId,
    DateTimeOffset SubmittedAt,
    Verdict Verdict,
    string Message,
    string CompilerOutput,
    int ExerciseVersion,
    int ReferenceVersion,
    int RulesVersion,
    string SourceCode,
    IReadOnlyList<TestResult> Tests);

public sealed record PixelImage(int Width, int Height, byte[] Rgb)
{
    public static PixelImage Create(int width, int height, Func<int, int, byte> value)
    {
        var bytes = new byte[checked(width * height * 3)];
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var v = value(x, y);
            var offset = (y * width + x) * 3;
            bytes[offset] = bytes[offset + 1] = bytes[offset + 2] = v;
        }
        return new PixelImage(width, height, bytes);
    }
}
