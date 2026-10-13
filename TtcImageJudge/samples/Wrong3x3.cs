using System.Drawing;
using System.Drawing.Imaging;
public static unsafe void Blur5x5(Bitmap imgSrc, Bitmap imgDest)
{
    var rect=new Rectangle(0,0,imgSrc.Width,imgSrc.Height);var a=imgSrc.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);var b=imgDest.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format24bppRgb);
    try{byte* p=(byte*)a.Scan0;byte* q=(byte*)b.Scan0;for(int y=1;y<imgSrc.Height-1;y++)for(int x=1;x<imgSrc.Width-1;x++){int sum=0;for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)sum+=p[(y+dy)*a.Stride+(x+dx)*3];byte v=(byte)(sum/9);byte* d=q+y*b.Stride+x*3;d[0]=d[1]=d[2]=v;}}finally{imgSrc.UnlockBits(a);imgDest.UnlockBits(b);}
}
