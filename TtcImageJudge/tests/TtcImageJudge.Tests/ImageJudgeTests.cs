using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using TtcImageJudge.Api;
using TtcImageJudge.Core;
using TtcImageJudge.Executor;

namespace TtcImageJudge.Tests;

public sealed class ImageJudgeTests
{
    [Fact]
    public void Reference_preserves_small_images_and_borders()
    {
        var small = PixelImage.Create(4, 3, (x, y) => (byte)(x + y * 10));
        Assert.Equal(small.Rgb, ImageJudge.ReferenceMean5x5(small).Rgb);
        var image = PixelImage.Create(7, 7, (x, y) => (byte)(x * 17 + y * 3));
        var output = ImageJudge.ReferenceMean5x5(image);
        for (var x = 0; x < 7; x++)
        { AssertPixelEqual(image, output, x, 0); AssertPixelEqual(image, output, x, 1); AssertPixelEqual(image, output, x, 5); AssertPixelEqual(image, output, x, 6); }
    }

    [Fact]
    public void Impulse_has_independently_calculated_center_value()
    {
        var input = PixelImage.Create(9, 9, (x, y) => (byte)(x == 4 && y == 4 ? 255 : 0));
        var output = ImageJudge.ReferenceMean5x5(input);
        Assert.Equal(10, output.Rgb[(4 * 9 + 4) * 3]); // 255 / 25, divisão inteira
        Assert.Equal(0, ImageJudge.Compare(output, output, 0).DifferentPixels);
    }

    [Fact]
    public void Exact_comparison_rejects_one_pixel_and_reports_coordinate()
    {
        var expected = PixelImage.Create(5, 4, (_, _) => 20);
        var actual = new PixelImage(5, 4, (byte[])expected.Rgb.Clone());
        actual.Rgb[(2 * 5 + 3) * 3 + 1] = 21;
        var difference = ImageJudge.Compare(expected, actual, 0);
        Assert.Equal(1, difference.DifferentPixels); Assert.Equal(3, difference.FirstX); Assert.Equal(2, difference.FirstY);
        Assert.Equal(5d, difference.Percentage);
    }

    [Theory]
    [InlineData("divide-by-9")]
    [InlineData("window-3x3")]
    [InlineData("changed-borders")]
    public void Independently_calculated_wrong_variants_are_rejected(string variant)
    {
        var input = PixelImage.Create(9, 9, (x, y) => (byte)((x * 37 + y * 19) % 256));
        var expected = ImageJudge.ReferenceMean5x5(input);
        var wrong = new PixelImage(9, 9, (byte[])input.Rgb.Clone());
        if (variant == "changed-borders") Array.Fill(wrong.Rgb, (byte)0, 0, 9 * 3);
        else
        {
            var radius = variant == "window-3x3" ? 1 : 2;
            var divisor = variant == "divide-by-9" ? 9 : 9;
            for (var y = radius; y < 9 - radius; y++) for (var x = radius; x < 9 - radius; x++)
            {
                var sum = 0; for (var dy = -radius; dy <= radius; dy++) for (var dx = -radius; dx <= radius; dx++)
                    sum += input.Rgb[((y + dy) * 9 + x + dx) * 3];
                var value = (byte)Math.Min(255, sum / divisor); var offset = (y * 9 + x) * 3;
                wrong.Rgb[offset] = wrong.Rgb[offset + 1] = wrong.Rgb[offset + 2] = value;
            }
        }
        Assert.True(ImageJudge.Compare(expected, wrong, 0).DifferentPixels > 0);
    }

    [Fact]
    public void Structured_requirement_analysis_does_not_accept_unsafe_keyword_alone()
    {
        var analyzer = new DirectMemoryAnalyzer();
        Assert.False(analyzer.Analyze("public static unsafe Bitmap Blur5x5(Bitmap img) => img;").Met);
        Assert.True(analyzer.Analyze("""
            public static unsafe Bitmap Blur5x5(Bitmap img) {
              BitmapData d = img.LockBits(default, default, default); byte* p=(byte*)d.Scan0;
              int s=d.Stride; img.UnlockBits(d); return img;
            }
            """).Met);
    }

    [Fact]
    public void Generated_runner_is_valid_csharp_syntax()
    {
        var request = new ExecuteRequest(Guid.NewGuid(), SeedData.InitialExercise, Samples.Reference);
        var diagnostics = CSharpSyntaxTree.ParseText(RunnerSource.Build(request)).GetDiagnostics()
            .Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.Empty(diagnostics);
    }

    [Fact]
    public async Task Sqlite_persists_history_and_seed_across_instances()
    {
        var root = Path.Combine(Path.GetTempPath(), "ttc-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                { ["Database:Path"] = Path.Combine(root, "judge.db") }).Build();
            var env = new FakeEnvironment(root);
            var first = new JudgeDatabase(config, env); await first.InitializeAsync();
            var exercise = (await first.ListExercisesAsync()).Single();
            var submission = new SubmissionView(Guid.NewGuid(), exercise.Id, DateTimeOffset.UtcNow,
                Verdict.InfrastructureFailure, "isolamento ausente", "", 1, 1, 1, "code", []);
            await first.SaveSubmissionAsync(submission);
            var second = new JudgeDatabase(config, env); await second.InitializeAsync();
            Assert.Equal(submission.Id, (await second.ListSubmissionsAsync(exercise.Id)).Single().Id);
        }
        finally { Directory.Delete(root, true); }
    }

    private static void AssertPixelEqual(PixelImage a, PixelImage b, int x, int y)
    { var offset = (y * a.Width + x) * 3; Assert.Equal(a.Rgb[offset], b.Rgb[offset]); }

    private sealed class FakeEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = root;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
