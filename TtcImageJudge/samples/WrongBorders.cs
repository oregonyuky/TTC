using System.Drawing;
using System.Drawing.Imaging;
public static unsafe Bitmap Blur5x5(Bitmap img)
{
    var output=new Bitmap(img.Width,img.Height,PixelFormat.Format24bppRgb);var rect=new Rectangle(0,0,img.Width,img.Height);var a=img.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format24bppRgb);var b=output.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format24bppRgb);
    try{byte* p=(byte*)a.Scan0;byte* q=(byte*)b.Scan0;for(int y=2;y<img.Height-2;y++)for(int x=2;x<img.Width-2;x++){int sum=0;for(int dy=-2;dy<=2;dy++)for(int dx=-2;dx<=2;dx++)sum+=p[(y+dy)*a.Stride+(x+dx)*3];byte v=(byte)(sum/25);byte* d=q+y*b.Stride+x*3;d[0]=d[1]=d[2]=v;}}finally{img.UnlockBits(a);output.UnlockBits(b);}return output;
}
