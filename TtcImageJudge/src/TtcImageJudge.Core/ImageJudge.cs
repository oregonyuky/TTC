namespace TtcImageJudge.Core;

public static class ImageJudge
{
    public static PixelDifference Compare(PixelImage expected, PixelImage actual, int tolerance)
    {
        if (expected.Width != actual.Width || expected.Height != actual.Height)
            return new PixelDifference(Math.Max(expected.Width * expected.Height, actual.Width * actual.Height),
                expected.Width * expected.Height, 100, 0, 0, 255);

        var different = 0;
        int? firstX = null, firstY = null;
        var maxDelta = 0;
        for (var y = 0; y < expected.Height; y++)
        for (var x = 0; x < expected.Width; x++)
        {
            var offset = (y * expected.Width + x) * 3;
            var pixelDiffers = false;
            for (var channel = 0; channel < 3; channel++)
            {
                var delta = Math.Abs(expected.Rgb[offset + channel] - actual.Rgb[offset + channel]);
                maxDelta = Math.Max(maxDelta, delta);
                pixelDiffers |= delta > tolerance;
            }
            if (!pixelDiffers) continue;
            different++;
            firstX ??= x;
            firstY ??= y;
        }

        var total = expected.Width * expected.Height;
        return new PixelDifference(different, total, total == 0 ? 0 : different * 100d / total,
            firstX, firstY, maxDelta);
    }

    public static PixelImage ReferenceMean5x5(PixelImage input)
    {
        var output = new PixelImage(input.Width, input.Height, (byte[])input.Rgb.Clone());
        for (var y = 2; y < input.Height - 2; y++)
        for (var x = 2; x < input.Width - 2; x++)
        {
            var sum = 0;
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++)
                sum += input.Rgb[((y + dy) * input.Width + x + dx) * 3];
            var value = (byte)(sum / 25);
            var offset = (y * input.Width + x) * 3;
            output.Rgb[offset] = output.Rgb[offset + 1] = output.Rgb[offset + 2] = value;
        }
        return output;
    }
}
