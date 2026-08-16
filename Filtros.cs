using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ProcessamentoImagens
{
    class Filtros
    {
        //sem acesso direto a memoria
        public static void convert_to_gray(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int r, g, b;
            Int32 gs;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //obtendo a cor do pixel
                    Color cor = imageBitmapSrc.GetPixel(x, y);

                    r = cor.R;
                    g = cor.G;
                    b = cor.B;
                    gs = (Int32)(r * 0.2990 + g * 0.5870 + b * 0.1140);

                    //nova cor
                    Color newcolor = Color.FromArgb(gs, gs, gs);

                    imageBitmapDest.SetPixel(x, y, newcolor);
                }
            }
        }

        //sem acesso direito a memoria
        public static void negativo(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int r, g, b;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    //obtendo a cor do pixel
                    Color cor = imageBitmapSrc.GetPixel(x, y);

                    r = cor.R;
                    g = cor.G;
                    b = cor.B;

                    //nova cor
                    Color newcolor = Color.FromArgb(255 - r, 255 - g, 255 - b);

                    imageBitmapDest.SetPixel(x, y, newcolor);
                }
            }
        }

        //com acesso direto a memória
        public static void convert_to_grayDMA(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            Int32 gs;

            //lock dados bitmap origem
            BitmapData bitmapDataSrc = imageBitmapSrc.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            //lock dados bitmap destino
            BitmapData bitmapDataDst = imageBitmapDest.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int padding = bitmapDataSrc.Stride - (width * pixelSize);

            unsafe
            {
                byte* src = (byte*)bitmapDataSrc.Scan0.ToPointer();
                byte* dst = (byte*)bitmapDataDst.Scan0.ToPointer();

                int r, g, b;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        b = *(src++); //está armazenado dessa forma: b g r 
                        g = *(src++);
                        r = *(src++);
                        gs = (Int32)(r * 0.2990 + g * 0.5870 + b * 0.1140);
                        *(dst++) = (byte)gs;
                        *(dst++) = (byte)gs;
                        *(dst++) = (byte)gs;
                    }
                    src += padding;
                    dst += padding;
                }
            }
            //unlock imagem origem
            imageBitmapSrc.UnlockBits(bitmapDataSrc);
            //unlock imagem destino
            imageBitmapDest.UnlockBits(bitmapDataDst);
        }

        //com acesso direito a memoria
        public static void negativoDMA(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;

            //lock dados bitmap origem 
            BitmapData bitmapDataSrc = imageBitmapSrc.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            //lock dados bitmap destino
            BitmapData bitmapDataDst = imageBitmapDest.LockBits(new Rectangle(0, 0, width, height),
                ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int padding = bitmapDataSrc.Stride - (width * pixelSize);

            unsafe
            {
                byte* src1 = (byte*)bitmapDataSrc.Scan0.ToPointer();
                byte* dst = (byte*)bitmapDataDst.Scan0.ToPointer();

                int r, g, b;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        b = *(src1++); //está armazenado dessa forma: b g r 
                        g = *(src1++);
                        r = *(src1++);

                        *(dst++) = (byte)(255 - b);
                        *(dst++) = (byte)(255 - g);
                        *(dst++) = (byte)(255 - r);
                    }
                    src1 += padding;
                    dst += padding;
                }
            }
            //unlock imagem origem 
            imageBitmapSrc.UnlockBits(bitmapDataSrc);
            //unlock imagem destino
            imageBitmapDest.UnlockBits(bitmapDataDst);
        }
        public static void espelharHorizontalDMA(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bitmapDataSrc = imageBitmapSrc.LockBits(new Rectangle(0, 0, width, height),ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bitmapDataDst = imageBitmapDest.LockBits(new Rectangle(0, 0, width, height),ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int padding = bitmapDataSrc.Stride - (width * pixelSize);
            unsafe
            {
                byte* src = (byte*)bitmapDataSrc.Scan0.ToPointer();
                byte* dst = (byte*)bitmapDataDst.Scan0.ToPointer();
                int b, g, r;
                for(int i = 0; i < height; i++)
                {
                    for(int j = width - 1; j >= 0; j--)
                    {
                        byte* aux = src + (i * bitmapDataSrc.Stride) + (j * pixelSize);
                        b = *(aux++); //está armazenado dessa forma: b g r 
                        g = *(aux++);
                        r = *(aux++);

                        *(dst++) = (byte)(b);
                        *(dst++) = (byte)(g);
                        *(dst++) = (byte)(r);
                    }
                    dst += padding;
                }
            }
            imageBitmapSrc.UnlockBits(bitmapDataSrc);
            imageBitmapDest.UnlockBits(bitmapDataDst);
        }
        public static void espelharVertical(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bitmapDataSrc = imageBitmapSrc.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb );
            BitmapData bitmapDataDst = imageBitmapDest.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int padding = bitmapDataSrc.Stride - (width * pixelSize);
            unsafe{
                byte* src = (byte*)bitmapDataSrc.Scan0.ToPointer();
                byte* dst = (byte*)bitmapDataDst.Scan0.ToPointer();
                int b, g, r;
                for(int i=height-1;i>=0;i--){
                    for(int j=0;j<width;j++){
                        byte *aux = src + (i * bitmapDataSrc.Stride) + (j * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);

                        *(dst++) = (byte)(b);
                        *(dst++) = (byte)(g);
                        *(dst++) = (byte)(r);
                    }
                    dst += padding;
                }
            }
            imageBitmapSrc.UnlockBits(bitmapDataSrc);
            imageBitmapDest.UnlockBits(bitmapDataDst);
        }
        public static void pretoBranco(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bitmapDataSrc = imageBitmapSrc.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bitmapDataDest = imageBitmapDest.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int padding = bitmapDataSrc.Stride - (width * pixelSize);
            int b, g, r;
            unsafe{
                byte* src = (byte*)bitmapDataSrc.Scan0.ToPointer();
                byte* dst = (byte*)bitmapDataDest.Scan0.ToPointer();
                for(int i=0;i<height;i++){
                    for(int j=0;j<width;j++){
                        byte* aux = src + (i * bitmapDataSrc.Stride) + (j * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        int media = (b + g + r) / 3;
                        if(media >= 128){
                            b = 255;
                            g = 255;
                            r = 255;
                        } else {
                            b = 0;
                            g = 0;
                            r = 0;
                        }
                        *(dst++) = (byte)(b);
                        *(dst++) = (byte)(g);
                        *(dst++) = (byte)(r);
                    }
                    dst += padding;
                }
            }
            imageBitmapSrc.UnlockBits(bitmapDataSrc);
            imageBitmapDest.UnlockBits(bitmapDataDest);
        }
        public static void rotacionar_90(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bitmapDataSrc = imageBitmapSrc.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bitmapDataDest = imageBitmapDest.LockBits(new Rectangle(0,0,height,width), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe{
                byte* src = (byte*)bitmapDataSrc.Scan0.ToPointer();
                byte* dst = (byte*)bitmapDataDest.Scan0.ToPointer();
                int b, g, r;
                for(int y = 0; y < height; y++){
                    for(int x = 0; x < width; x++){
                        byte* auxSrc = src + (y * bitmapDataSrc.Stride) + (x * pixelSize);
                        int novoX = height - 1 - y;
                        int novoY = x;
                        byte* auxDst = dst + (novoY * bitmapDataDest.Stride) + (novoX * pixelSize);
                        b = *(auxSrc++);
                        g = *(auxSrc++);
                        r = *(auxSrc++);
                        *(auxDst++) = (byte)b;
                        *(auxDst++) = (byte)g;
                        *(auxDst++) = (byte)r;
                    }
                }
            }
            imageBitmapSrc.UnlockBits(bitmapDataSrc);
            imageBitmapDest.UnlockBits(bitmapDataDest);
        }
        public static void inverterVermelhoComAzul(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bds = imageBitmapSrc.LockBits(new Rectangle(0,0,width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bdd = imageBitmapDest.LockBits(new Rectangle(0,0,width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            unsafe{
                byte* src = (byte*)bds.Scan0.ToPointer();
                byte* dst = (byte*)bdd.Scan0.ToPointer();
                int padding = bds.Stride - (width * pixelSize);
                int b, g, r;
                for(int i=0;i<height;i++){
                    for(int j=0;j<width;j++){
                        byte* aux = src + (i * bds.Stride) + (j * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        *(dst++) = (byte)r;
                        *(dst++) = (byte)g;
                        *(dst++) = (byte)b;
                    }
                    dst += padding;
                }
            }
            imageBitmapSrc.UnlockBits(bds);
            imageBitmapDest.UnlockBits(bdd);
        }
        public static void separarRed(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bmS = imageBitmapSrc.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe{
                int padding = bmS.Stride - (width * pixelSize);
                int b, r, g;
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                for(int i=0;i<height;i++){
                    for(int j=0;j<width;j++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        b = g = 0;
                        *(dst++) = (byte)b;
                        *(dst++) = (byte)g;
                        *(dst++) = (byte)r;
                    }
                    dst += padding;
                }

            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
        public static void separarGreen(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bmS = imageBitmapSrc.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe{
                int padding = bmS.Stride - (width * pixelSize);
                int b, r, g;
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                for(int i=0;i<height;i++){
                    for(int j=0;j<width;j++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        b = r = 0;
                        *(dst++) = (byte)b;
                        *(dst++) = (byte)g;
                        *(dst++) = (byte)r;
                    }
                    dst += padding;
                }

            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
        public static void separarBlue(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bmS = imageBitmapSrc.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe{
                int padding = bmS.Stride - (width * pixelSize);
                int b, r, g;
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                for(int i=0;i<height;i++){
                    for(int j=0;j<width;j++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        r = g = 0;
                        *(dst++) = (byte)b;
                        *(dst++) = (byte)g;
                        *(dst++) = (byte)r;
                    }
                    dst += padding;
                }

            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
        public static void espelharDiagonal(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bmS = imageBitmapSrc.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits(new Rectangle(0,0,height,width), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            unsafe{
                int b, r, g;
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                for(int i=0;i<height;i++){
                    for(int j=0;j<width;j++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        byte* auxN = dst + (j * bmD.Stride) + (i * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        *(auxN++) = (byte)b;
                        *(auxN++) = (byte)g;
                        *(auxN++) = (byte)r;
                    }
                }
            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
        public static void dividirImagem(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            int pixelSize = 3;
            BitmapData bmS = imageBitmapSrc.LockBits(new Rectangle(0,0,width,height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits(new Rectangle(0,0,width,height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            unsafe{
                int b, r, g;
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                int h2 = height/2;
                int w2 = width/2;
                //Superior esquerdo → inferior direito
                for(int i=0, h2N=height/2;i<h2;i++, h2N++){
                    for(int j=0, w2N=width/2;j<w2;j++, w2N++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        byte* auxN = dst + (h2N * bmD.Stride) + (w2N * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        *(auxN++) = (byte)b;
                        *(auxN++) = (byte)g;
                        *(auxN++) = (byte)r;
                    }
                }
                //Superior direito → inferior esquerdo
                for(int i=0, h2N=height/2;i<h2;i++, h2N++){
                    for(int j=w2, w2N=0;j<width;j++, w2N++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        byte* auxN = dst + (h2N * bmD.Stride) + (w2N * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        *(auxN++) = (byte)b;
                        *(auxN++) = (byte)g;
                        *(auxN++) = (byte)r;
                    }
                }
                //Inferior esquerdo → superior direito
                for(int i=h2, h2N=0;i<height;i++, h2N++){
                    for(int j=0, w2N=w2;j<w2;j++, w2N++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        byte* auxN = dst + (h2N * bmD.Stride) + (w2N * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        *(auxN++) = (byte)b;
                        *(auxN++) = (byte)g;
                        *(auxN++) = (byte)r;
                    }
                }
                //Inferior direito → superior esquerdo
                for(int i=h2, h2N=0;i<height;i++, h2N++){
                    for(int j=w2, w2N=0;j<width;j++, w2N++){
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        byte* auxN = dst + (h2N * bmD.Stride) + (w2N * pixelSize);
                        b = *(aux++);
                        g = *(aux++);
                        r = *(aux++);
                        *(auxN++) = (byte)b;
                        *(auxN++) = (byte)g;
                        *(auxN++) = (byte)r;
                    }
                }
            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
    }
}
