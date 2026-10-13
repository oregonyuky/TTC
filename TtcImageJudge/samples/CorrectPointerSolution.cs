using System.Drawing;
using System.Drawing.Imaging;

public static unsafe void Blur5x5(Bitmap imgSrc, Bitmap imgDest)
{
    var rectangle = new Rectangle(0, 0, imgSrc.Width, imgSrc.Height);
    var sourceData = imgSrc.LockBits(rectangle, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    var outputData = imgDest.LockBits(rectangle, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
    try
    {
        byte* source = (byte*)sourceData.Scan0;
        byte* target = (byte*)outputData.Scan0;
        for (var y = 2; y < imgSrc.Height - 2; y++)
        for (var x = 2; x < imgSrc.Width - 2; x++)
        {
            var sum = 0;
            for (var dy = -2; dy <= 2; dy++)
            for (var dx = -2; dx <= 2; dx++) sum += source[(y + dy) * sourceData.Stride + (x + dx) * 3];
            var value = (byte)(sum / 25);
            var pixel = target + y * outputData.Stride + x * 3;
            pixel[0] = pixel[1] = pixel[2] = value;
        }
    }
    finally { imgSrc.UnlockBits(sourceData); imgDest.UnlockBits(outputData); }
}
