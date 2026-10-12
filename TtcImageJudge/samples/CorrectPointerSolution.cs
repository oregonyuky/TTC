using System.Drawing;
using System.Drawing.Imaging;

public static unsafe Bitmap Blur5x5(Bitmap img)
{
    var output = img.Clone(new Rectangle(0, 0, img.Width, img.Height), PixelFormat.Format24bppRgb);
    var rectangle = new Rectangle(0, 0, img.Width, img.Height);
    var sourceData = img.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    var outputData = output.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
    try
    {
        byte* source = (byte*)sourceData.Scan0;
        byte* target = (byte*)outputData.Scan0;
        for (var y = 2; y < img.Height - 2; y++)
        for (var x = 2; x < img.Width - 2; x++)
        {
            var sum = 0;
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++) sum += source[(y + dy) * sourceData.Stride + (x + dx) * 3];
            var value = (byte)(sum / 25);
            var pixel = target + y * outputData.Stride + x * 3;
            pixel[0] = pixel[1] = pixel[2] = value;
        }
    }
    finally { img.UnlockBits(sourceData); output.UnlockBits(outputData); }
    return output;
}
