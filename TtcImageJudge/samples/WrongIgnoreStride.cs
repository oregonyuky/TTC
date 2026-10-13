using System.Drawing;
using System.Drawing.Imaging;
public static unsafe void Blur5x5(Bitmap imgSrc, Bitmap imgDest)
{
    var rect=new Rectangle(0,0,imgSrc.Width,imgSrc.Height);var a=imgSrc.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);var b=imgDest.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format24bppRgb);
    try{byte* p=(byte*)a.Scan0;byte* q=(byte*)b.Scan0;int stride=a.Stride;for(int y=2;y<imgSrc.Height-2;y++)for(int x=2;x<imgSrc.Width-2;x++){int sum=0;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)sum+=p[(y+dy)*imgSrc.Width*3+(x+dx)*3];byte v=(byte)(sum/25);byte* d=q+y*imgSrc.Width*3+x*3;d[0]=d[1]=d[2]=v;}}finally{imgSrc.UnlockBits(a);imgDest.UnlockBits(b);}
}
