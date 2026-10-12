using System.Drawing;
using System.Drawing.Imaging;
public static unsafe Bitmap Blur5x5(Bitmap img)
{
    var d = img.LockBits(new Rectangle(0,0,img.Width,img.Height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
    byte* p=(byte*)d.Scan0; var stride=d.Stride; img.UnlockBits(d);
    while (true) { }
}
