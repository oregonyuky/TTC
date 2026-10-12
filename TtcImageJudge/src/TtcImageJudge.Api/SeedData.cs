using TtcImageJudge.Core;

namespace TtcImageJudge.Api;

public static class SeedData
{
    public static ExerciseDetails InitialExercise { get; } = new(
        Guid.Parse("b0f45a61-30e1-4e04-a90c-110ab7e6f117"), "media-5x5-memoria-direta",
        "Filtro passa-baixa por média 5 × 5",
        "Implemente um algoritmo com acesso direto à memória que realize a filtragem espacial passa-baixa por média em uma imagem em tons de cinza, usando uma vizinhança 5 × 5.",
        "A menção original a “K = 9” não tem significado esclarecido e não é aplicada neste exercício.",
        "Intermediário", 1, 1, 1,
        new ExerciseRules("public static unsafe Bitmap Blur5x5(Bitmap img)",
            "Bitmap 24bppRgb; R, G e B iguais",
            "Processar somente vizinhanças completas; preservar duas linhas e colunas em cada borda",
            "Soma inteira dos 25 valores, divisão inteira por 25; resultado já no intervalo 0–255",
            true, 0, 2_000, 256, true),
        Samples.Starter,
        [
            Test("Constante 1×1", "constant", 1, 1, 1), Test("Constante menor que a janela", "constant", 4, 3, 2),
            Test("Gradiente 7×7", "gradient", 7, 7, 3), Test("Impulso central", "impulse", 9, 9, 4),
            Test("Padrão alternado", "checkerboard", 11, 8, 5), Test("Ruído determinístico", "noise", 13, 10, 20261012),
            Test("Stride com padding", "gradient", 5, 7, 7)
        ]);

    private static TestCaseDefinition Test(string name, string pattern, int width, int height, int seed) =>
        new(Guid.NewGuid(), name, pattern, width, height, seed, ExpectedOutputMode.ReferenceImplementation,
            ReferenceCode: Samples.Reference, GeneratedForReferenceVersion: 0, GeneratedForRulesVersion: 0);
}

public static class Samples
{
    public const string Starter = """
        using System.Drawing;
        using System.Drawing.Imaging;

        public static unsafe Bitmap Blur5x5(Bitmap img)
        {
            // Crie uma saída 24bpp, use LockBits/Stride e preserve a entrada.
            throw new NotImplementedException();
        }
        """;

    public const string Reference = """
        using System.Drawing;
        using System.Drawing.Imaging;

        public static unsafe Bitmap Blur5x5(Bitmap img)
        {
            var output = img.Clone(new Rectangle(0, 0, img.Width, img.Height), PixelFormat.Format24bppRgb);
            var area = new Rectangle(0, 0, img.Width, img.Height);
            var sourceData = img.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var outputData = output.LockBits(area, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            try
            {
                byte* source = (byte*)sourceData.Scan0;
                byte* target = (byte*)outputData.Scan0;
                for (int y = 2; y < img.Height - 2; y++)
                for (int x = 2; x < img.Width - 2; x++)
                {
                    int sum = 0;
                    for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                        sum += source[(y + dy) * sourceData.Stride + (x + dx) * 3];
                    byte value = (byte)(sum / 25);
                    byte* pixel = target + y * outputData.Stride + x * 3;
                    pixel[0] = pixel[1] = pixel[2] = value;
                }
            }
            finally { img.UnlockBits(sourceData); output.UnlockBits(outputData); }
            return output;
        }
        """;
}
