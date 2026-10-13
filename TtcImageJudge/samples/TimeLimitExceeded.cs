using System.Drawing;
using System.Drawing.Imaging;
public static unsafe void Blur5x5(Bitmap imgSrc, Bitmap imgDest)
{
    var d = imgSrc.LockBits(new Rectangle(0,0,imgSrc.Width,imgSrc.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    byte* p=(byte*)d.Scan0; var stride=d.Stride; imgSrc.UnlockBits(d);
    while (true) { }
}
