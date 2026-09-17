using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;  

namespace ProcessamentoImagens
{
    public class ObjetoSegmentado
    {
        public int Numero { get; set; }
        public int Area { get; set; }
        public double Largura { get; set; }
        public double Altura { get; set; }
    }

    class Filtros
    {
        static int pixelSize = 3;
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

        public static Bitmap segmentar4Conectados(Bitmap imageBitmapSrc, out List<ObjetoSegmentado> objetos)
        {
            return segmentarConectados(imageBitmapSrc, false, out objetos);
        }

        public static Bitmap segmentar8Conectados(Bitmap imageBitmapSrc, out List<ObjetoSegmentado> objetos)
        {
            return segmentarConectados(imageBitmapSrc, true, out objetos);
        }

        private static Bitmap segmentarConectados(Bitmap imageBitmapSrc, bool usarOitoConectados, out List<ObjetoSegmentado> objetos)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            bool[,] visitado = new bool[width, height];
            Bitmap imageBitmapDest = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            objetos = new List<ObjetoSegmentado>();

            // --- Leitura rápida da imagem de origem para um array de bool ---
            bool[,] pixelUm = lerPixelsComoBool(imageBitmapSrc);

            // --- Buffer de saída em memória (preenchido em RAM, sem SetPixel) ---
            BitmapData destData = imageBitmapDest.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            int strideDest = destData.Stride;
            byte[] bufferDest = new byte[strideDest * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (visitado[x, y] || !pixelUm[x, y])
                        continue;

                    int numero = objetos.Count + 1;
                    Queue<Point> fila = new Queue<Point>();
                    List<Point> pixels = new List<Point>();
                    int minX = x, maxX = x, minY = y, maxY = y;

                    visitado[x, y] = true;
                    fila.Enqueue(new Point(x, y));

                    while (fila.Count > 0)
                    {
                        Point pixel = fila.Dequeue();
                        pixels.Add(pixel);
                        minX = Math.Min(minX, pixel.X);
                        maxX = Math.Max(maxX, pixel.X);
                        minY = Math.Min(minY, pixel.Y);
                        maxY = Math.Max(maxY, pixel.Y);

                        for (int deslocamentoY = -1; deslocamentoY <= 1; deslocamentoY++)
                        {
                            for (int deslocamentoX = -1; deslocamentoX <= 1; deslocamentoX++)
                            {
                                if ((deslocamentoX == 0 && deslocamentoY == 0) ||
                                    (!usarOitoConectados && Math.Abs(deslocamentoX) + Math.Abs(deslocamentoY) != 1))
                                    continue;

                                int vizinhoX = pixel.X + deslocamentoX;
                                int vizinhoY = pixel.Y + deslocamentoY;
                                if (vizinhoX < 0 || vizinhoX >= width || vizinhoY < 0 || vizinhoY >= height ||
                                    visitado[vizinhoX, vizinhoY] || !pixelUm[vizinhoX, vizinhoY])
                                    continue;

                                visitado[vizinhoX, vizinhoY] = true;
                                fila.Enqueue(new Point(vizinhoX, vizinhoY));
                            }
                        }
                    }

                    Color corObjeto = obterCorObjeto(numero);
                    foreach (Point pixel in pixels)
                    {
                        int offset = pixel.Y * strideDest + pixel.X * 3;
                        bufferDest[offset]     = corObjeto.B;
                        bufferDest[offset + 1] = corObjeto.G;
                        bufferDest[offset + 2] = corObjeto.R;
                    }

                    objetos.Add(new ObjetoSegmentado
                    {
                        Numero = numero,
                        Area = pixels.Count,
                        // A distancia entre as coordenadas extremas recebe 1 para representar os dois pixels inclusivos.
                        Largura = distanciaEuclidiana(minX, minY, maxX, minY) + 1,
                        Altura = distanciaEuclidiana(minX, minY, minX, maxY) + 1
                    });
                }
            }

            Marshal.Copy(bufferDest, 0, destData.Scan0, bufferDest.Length);
            imageBitmapDest.UnlockBits(destData);

            return imageBitmapDest;
        }

        // Le a imagem inteira uma unica vez e devolve uma matriz de bool (true = pixel do objeto).
        private static bool[,] lerPixelsComoBool(Bitmap imageBitmapSrc)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            bool[,] resultado = new bool[width, height];

            BitmapData srcData = imageBitmapSrc.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);

            int stride = srcData.Stride;
            int bytesTotais = stride * height;
            byte[] buffer = new byte[bytesTotais];
            Marshal.Copy(srcData.Scan0, buffer, 0, bytesTotais);
            imageBitmapSrc.UnlockBits(srcData);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int offset = y * stride + x * 3;
                    byte b = buffer[offset];
                    byte g = buffer[offset + 1];
                    byte r = buffer[offset + 2];
                    resultado[x, y] = ehPixelUm(r, g, b);
                }
            }

            return resultado;
        }

        // No filtro preto-branco do projeto, preto (0) representa o objeto a ser segmentado (valor binario 1).
        private static bool ehPixelUm(byte r, byte g, byte b)
        {
            return r < 128 && g < 128 && b < 128;
        }

        private static double distanciaEuclidiana(int x1, int y1, int x2, int y2)
        {
            return Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }

        private static Color obterCorObjeto(int numero)
        {
            // A codificacao usa os tres canais para evitar repetir cores mesmo com muitos objetos.
            int indice = numero - 1;
            int vermelho = 32 + ((indice % 192) * 73 % 192);
            int verde = 32 + (((indice / 192) % 192) * 73 % 192);
            int azul = 32 + (((indice / (192 * 192)) % 192) * 73 % 192);
            return Color.FromArgb(vermelho, verde, azul);
        }

        public static void ReduzirMetadeDMA(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            BitmapData bmS = imageBitmapSrc.LockBits( new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits( new Rectangle(0, 0, width / 2, height / 2), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dest = (byte*)bmD.Scan0.ToPointer();
                int strideS = bmS.Stride;
                int strideD = bmD.Stride;
                for (int y = 0; y < height / 2; y++)
                {
                    for (int x = 0; x < width / 2; x++)
                    {
                        int srcX = x * 2;
                        int srcY = y * 2;
                        byte* p1 = src + srcY * strideS + srcX * 3;
                        byte* p2 = src + srcY * strideS + (srcX + 1) * 3;
                        byte* p3 = src + (srcY + 1) * strideS + srcX * 3;
                        byte* p4 = src + (srcY + 1) * strideS + (srcX + 1) * 3;
                        byte blue = (byte)( (p1[0] + p2[0] + p3[0] + p4[0]) / 4);
                        byte green = (byte)( (p1[1] + p2[1] + p3[1] + p4[1]) / 4);
                        byte red = (byte)( (p1[2] + p2[2] + p3[2] + p4[2]) / 4);
                        byte* pd = dest + y * strideD + x * 3;
                        pd[0] = blue;
                        pd[1] = green;
                        pd[2] = red;
                    }
                }
            }

            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
        public static void ReduzirMetade(Bitmap imageBitmapSrc, Bitmap imageBitmapDest){
            int height = imageBitmapSrc.Height;
            int width = imageBitmapSrc.Width;
            for(int i=0;i<height;i+=2){
                for(int j=0;j<width;j+=2){
                    Color cor1 = imageBitmapSrc.GetPixel(j, i);
                    Color cor2 = imageBitmapSrc.GetPixel(j,i+1);
                    Color cor3 = imageBitmapSrc.GetPixel(j+1,i);
                    Color cor4 = imageBitmapSrc.GetPixel(j+1,i+1);
                    int r = (cor1.R + cor2.R + cor3.R + cor4.R)/4;
                    int g = (cor1.G + cor2.G + cor3.G + cor4.G)/4;
                    int b = (cor1.B + cor2.B + cor3.B + cor4.B)/4;
                    Color newCor = Color.FromArgb(r, g, b);
                    imageBitmapDest.SetPixel(j/2, i/2, newCor);
                }
            }
        }


        public static void ReduzirEscalaResolucaoCinzaDadoValorDMA(Bitmap imageBitmapSrc, Bitmap imageBitmapDest, int valor)
        {
            int height = imageBitmapSrc.Height;
            int width = imageBitmapDest.Width;
            int pixelSize = 3;

            BitmapData bmS = imageBitmapSrc.LockBits( new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits( new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                int padding = bmD.Stride - width * pixelSize;
                for (int i = 0; i < height; i++)
                {
                    for (int j = 0; j < width; j++)
                    {
                        byte* aux = src + (i * bmS.Stride) + (j * pixelSize);
                        int b = *(aux++);
                        int g = *(aux++);
                        int r = *(aux++);
                        int gr = (int)(r * 0.299 + g * 0.587 + b * 0.114);

                        int novo = calcularNovoGr(gr, valor);

                        *(dst++) = (byte)novo;
                        *(dst++) = (byte)novo;
                        *(dst++) = (byte)novo;
                    }
                    dst += padding;
                }
            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
        public static void ReduzirEscalaResolucaoCinzaDadoValor(Bitmap imageBitmapSrc, Bitmap imageBitmapDest, int valor){
            int height = imageBitmapSrc.Height;
            int width = imageBitmapSrc.Width;
            for(int i=0;i<height;i++){
                for(int j=0;j<width;j++){
                    Color cor = imageBitmapSrc.GetPixel(j, i);
                    int r = cor.R;
                    int g = cor.G;
                    int b = cor.B;
                    int gr = (int)(r * 0.299 + g * 0.587 + b * 0.114);
                    int novo = calcularNovoGr(gr, valor);
                    Color newCor = Color.FromArgb(novo, novo, novo);
                    imageBitmapDest.SetPixel(j, i, newCor);
                }
            }
        }

        public static int calcularNovoGr(int gr, int valor)
        {
            int i = 0;
            while (gr >= i * 256 / valor) i++;
            return (i - 1) * 255 / (valor - 1);
        }

        public static void contourFollowing(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int height = imageBitmapSrc.Height;
            int width = imageBitmapSrc.Width;
            // Índices em sentido anti-horário: SO, S, SE, L, NE, N, NO, O.
            int[] y = { 1, 1, 1, 0, -1, -1, -1, 0 };
            int[] x = { -1, 0, 1, 1, 1, 0, -1, -1 };
            bool[,] visitado = new bool[height, width];
            BitmapData bmS = imageBitmapSrc.LockBits( new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits( new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dest = (byte*)bmD.Scan0.ToPointer();

                // O destino começa branco para mostrar somente os pixels do contorno.
                for (int linha = 0; linha < height; linha++)
                    for (int coluna = 0; coluna < width; coluna++)
                    {
                        byte* pixel = getByte(dest, bmD, linha, coluna);
                        pixel[0] = pixel[1] = pixel[2] = 255;
                    }

                for (int l = 0; l < height; l++)
                    for (int c = 0; c < width; c++)
                    {
                        // Um contorno começa em preto com fundo à esquerda.
                        if (visitado[l, c] || !isT(src, bmS, l, c) || (c > 0 && isT(src, bmS, l, c - 1))) continue;
                        int i = l, j = c;
                        int fundoI = i, fundoJ = j - 1;
                        int primeiroI = -1, primeiroJ = -1;
                        HashSet<long> estados = new HashSet<long>();
                        bool flag = true;
                        while (flag)
                        {
                            int direcaoFundo = 0;
                            while (direcaoFundo < 8 && (i + y[direcaoFundo] != fundoI || j + x[direcaoFundo] != fundoJ))
                                direcaoFundo++;

                            long estado = (((long)i * width + j) << 3) | (uint)direcaoFundo;
                            if (!estados.Add(estado))
                            {
                                flag = false;
                            }
                            else
                            {

                                visitado[i, j] = true;
                                byte* contorno = getByte(dest, bmD, i, j);
                                contorno[0] = contorno[1] = contorno[2] = 0;

                                int anteriorI = i, anteriorJ = j;
                                int direcaoEscolhida = -1;
                                byte* auxDest = null;

                                bool flag1 = true;
                                // Os mesmos oito ifs, testados a partir do vizinho de fundo.
                                for (int tentativa = 1; tentativa <= 8 && flag1; tentativa++)
                                {
                                    int direcao = (direcaoFundo + tentativa) % 8;
                                    if (direcao == 0 && i < height - 1 && j > 0 && isT(src, bmS, i + 1, j - 1))
                                    {
                                        auxDest = getByte(dest, bmD, ++i, --j);
                                    }
                                    else if (direcao == 1 && i < height - 1 && isT(src, bmS, i + 1, j))
                                    {
                                        auxDest = getByte(dest, bmD, ++i, j);
                                    }
                                    else if (direcao == 2 && i < height - 1 && j < width - 1 && isT(src, bmS, i + 1, j + 1))
                                    {
                                        auxDest = getByte(dest, bmD, ++i, ++j);
                                    }
                                    else if (direcao == 3 && j < width - 1 && isT(src, bmS, i, j + 1))
                                    {
                                        auxDest = getByte(dest, bmD, i, ++j);
                                    }
                                    else if (direcao == 4 && i > 0 && j < width - 1 && isT(src, bmS, i - 1, j + 1))
                                    {
                                        auxDest = getByte(dest, bmD, --i, ++j);
                                    }
                                    else if (direcao == 5 && i > 0 && isT(src, bmS, i - 1, j))
                                    {
                                        auxDest = getByte(dest, bmD, --i, j);
                                    }
                                    else if (direcao == 6 && i > 0 && j > 0 && isT(src, bmS, i - 1, j - 1))
                                    {
                                        auxDest = getByte(dest, bmD, --i, --j);
                                    }
                                    else if (direcao == 7 && j > 0 && isT(src, bmS, i, j - 1))
                                    {
                                        auxDest = getByte(dest, bmD, i, --j);
                                    }

                                    if (auxDest != null)
                                    {
                                        direcaoEscolhida = direcao;
                                        flag1 = false;
                                    }
                                }

                                if (direcaoEscolhida < 0 || (anteriorI == l && anteriorJ == c && i == primeiroI && j == primeiroJ))
                                {
                                    flag = false;
                                } else { 

                                    if (primeiroI < 0)
                                    {
                                        primeiroI = i;
                                        primeiroJ = j;
                                    }

                                    int direcaoAnterior = (direcaoEscolhida + 7) % 8;
                                    fundoI = anteriorI + y[direcaoAnterior];
                                    fundoJ = anteriorJ + x[direcaoAnterior];
                                }
                            }
                        }
                    }
            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }

        public static unsafe byte* getByte( byte* src, BitmapData bmS, int i, int j)
        {
            return src + (i * bmS.Stride) + (j * 3);
        }

        public static unsafe int getBGR(byte* aux, char bgr)
        {
            if (bgr == 'b') return *(aux);
            if (bgr == 'g') return *(aux + 1);
            if (bgr == 'r') return *(aux + 2);
            return 0;
        }

        public static bool ehPreto(int b, int g, int r)
        {
            return b < 128 && g < 128 && r < 128;
        }

        public static unsafe bool isT( byte* src, BitmapData bmS, int i, int j) {
            return ehPreto(
                getBGR(getByte(src, bmS, i, j), 'b'),
                getBGR(getByte(src, bmS, i, j), 'g'),
                getBGR(getByte(src, bmS, i, j), 'r')
            );
        }

        public unsafe static byte* p0(int i, int j, BitmapData bd)
        {
            return (j < bd.Width - 1) ? (byte*)(i * bd.Stride) + ((j + 1) * pixelSize) : null;
        }

        public unsafe static byte* p1(int i, int j, BitmapData bd)
        {
            return (i > 0 && j < bd.Width - 1) ? (byte*)((i - 1) * bd.Stride) + ((j + 1) * pixelSize) : null;
        }

        public unsafe static byte* p2(int i, int j, BitmapData bd)
        {
            return (i > 0) ? (byte*)((i - 1) * bd.Stride) + (j * pixelSize) : null;
        }

        public unsafe static byte* p3(int i, int j, BitmapData bd)
        {
            return (i > 0 && j > 0) ? (byte*)((i - 1) * bd.Stride) + ((j - 1) * pixelSize) : null;
        }

        public unsafe static byte* p4(int i, int j, BitmapData bd)
        {
            return (j > 0) ? (byte*)(i * bd.Stride) + ((j - 1) * pixelSize) : null;
        }

        public unsafe static byte* p5(int i, int j, BitmapData bd)
        {
            return (i < bd.Height - 1 && j > 0) ? (byte*)((i + 1) * bd.Stride) + ((j - 1) * pixelSize) : null;
        }

        public unsafe static byte* p6(int i, int j, BitmapData bd)
        {
            return (i < bd.Height - 1) ? (byte*)((i + 1) * bd.Stride) + (j * pixelSize) : null;
        }

        public unsafe static byte* p7(int i, int j, BitmapData bd)
        {
            return (i < bd.Height - 1 && j < bd.Width - 1) ? (byte*)((i + 1) * bd.Stride) + ((j + 1) * pixelSize) : null;
        }

        public unsafe static byte* getP(int i, int j, BitmapData bd)
        {
            return (i >= 0 && i < bd.Height && j >= 0 && j < bd.Width) ? (byte*)(i * bd.Stride) + (j * pixelSize) : null;
        }

        public unsafe static void binarizarEntrada(BitmapData bd)
        {
            int width = bd.Width;
            int height = bd.Height;
            byte* src = (byte*)bd.Scan0.ToPointer();
            for(int i = 0; i < height; i++)
            {
                for(int j = 0; j < width; j++)
                {
                    byte* aux = src + (i*bd.Stride) + (j*pixelSize);
                    int cor;
                    if ((aux[0] + aux[1] + aux[2]) / 3 < 128) cor = 0;
                    else cor = 255;
                    *(aux++) = (byte)cor;
                    *(aux++) = (byte)cor;
                    *(aux++) = (byte)cor;
                }
            }
        }
        public unsafe static void branquearSaida(BitmapData bd)
        {
            int width = bd.Width;
            int height = bd.Height;
            byte* src = (byte*)bd.Scan0.ToPointer();
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    byte* aux = src + (i * bd.Stride) + (j * pixelSize);
                    int cor = 255;
                    *(aux++) = (byte)cor;
                    *(aux++) = (byte)cor;
                    *(aux++) = (byte)cor;
                }
            }
        }

        private unsafe static bool ehBranco(byte* pixel)
        {
            return pixel[0] == 255 && pixel[1] == 255 && pixel[2] == 255;
        }
        private unsafe static bool ehPreto(byte* pixel)
        {
            return pixel[0] == 0 && pixel[1] == 255 && pixel[2] == 255;
        }

        private unsafe static void marcarPixel(byte* pixel)
        {
            pixel[0] = 255; pixel[1] = 0;  pixel[2] = 0;
        }

        public static bool ehConectividade4(int x1, int y1, int x2, int y2){
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2) == 1;
        }
        public unsafe static void acharPos(int i, int j, int direcao, BitmapData bd, List<int> dir, Pilha p){
            byte*[] pixelsVet = new byte*[8] {p0(i,j,bd),p1(i,j,bd),p2(i,j,bd),p3(i,j,bd),p4(i,j,bd),p5(i,j,bd),p6(i,j,bd),p7(i,j,bd)};
            byte* pixelAtual = (byte*)bd.Scan0.ToPointer() + i*bd.Stride + j*pixelSize;
            for(int t=1;t<=8;t++){
                int idx = (direcao + t)%8;
                if(pixelsVet[idx] != null && ehPreto(pixelsVet[idx])){
                    int idxAnt = (idx+7)%8;
                    if(pixelsVet[idxAnt]!=null && ehBranco(pixelsVet[idxAnt])){
                        int x=-1, y=-1;
                        switch (idxAnt)
                        {
                            case 0: x = j + 1; y = i; break; // direita
                            case 1: x = j + 1; y = i - 1; break; // acima direita
                            case 2: x = j; y = i - 1; break; // acima
                            case 3: x = j - 1; y = i - 1; break; // acima esquerda
                            case 4: x = j - 1; y = i; break; // esquerda
                            case 5: x = j - 1; y = i + 1; break; // abaixo esquerda
                            case 6: x = j; y = i + 1; break; // abaixo
                            case 7: x = j + 1; y = i + 1; break; // abaixo direita
                        };
                        if(ehConectividade4(x, y, i, j)){
                            dir.Insert(0, (idxAnt+4)%8);
                            p.push(new Info(x, y));
                        } else {
                            int posAnt = (idx+7)%8;
                            int posProx = (idx+1)%8;
                            if((pixelsVet[posAnt]!=null && ehBranco(pixelsVet[posAnt])) || (pixelsVet[posProx]!=null && ehBranco(pixelsVet[posProx]))){
                                dir.Insert(0, (idxAnt+4)%8);
                                p.push(new Info(x, y));
                            }
                        }
                    }
                }
            }
        }

        public unsafe static bool marcado(byte* aux){
            return aux[0]==255 && aux[1]==0 && aux[2]==0;
        }
        public unsafe static void escreverMarcadosNaImagemDestino(BitmapData bmS, BitmapData bmD){
            int width = bmD.Width;
            int height = bmD.Height;
            int padding = bmS.Stride - width*pixelSize;
            byte* src = (byte*)bmS.Scan0.ToPointer();
            byte* dst = (byte*)bmD.Scan0.ToPointer();
            for(int i=0;i<height;i++){
                for(int j=0;j<width;j++){
                    if(marcado(src)){
                        *(dst++) = 0;
                        *(dst++) = 0;
                        *(dst++) = 0;
                    } else {
                        *(dst++) = 255;
                        *(dst++) = 255;
                        *(dst++) = 255;
                    }
                    src += 3;
                }
                src += padding;
                dst += padding;
            }
        }
        public static void contourFollowingDMA(Bitmap imageBitmapSrc, Bitmap imageBitmapDest)
        {
            int width = imageBitmapSrc.Width;
            int height = imageBitmapSrc.Height;
            BitmapData bmS = imageBitmapSrc.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            BitmapData bmD = imageBitmapDest.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            int padding = bmS.Stride - (width * pixelSize);
            List<int> dir = new List<int>();
            Pilha p = new Pilha();
            unsafe
            {
                byte* src = (byte*)bmS.Scan0.ToPointer();
                byte* dst = (byte*)bmD.Scan0.ToPointer();
                binarizarEntrada(bmS);
                branquearSaida(bmD);
                for(int i = 0; i < height; i++)
                {
                    for(int j = 0; j < width; j++)
                    {
                        byte* pixelAtual = src + i * bmS.Stride + j * pixelSize;
                        byte* pixelDireita = getP(i, j+1, bmS);
                        if(ehBranco(pixelAtual) && pixelDireita!=null && ehPreto(pixelDireita)){
                            byte* destino = pixelAtual;
                            byte* atualContorno = destino;
                            int iAtual = i, jAtual = j, direcao=4;
                            do{
                                marcarPixel(atualContorno);
                                acharPos(iAtual, jAtual, direcao, bmS, dir, p);
                                if(!p.isEmpty()){
                                    direcao = dir[0];
                                    Info info = p.pop();
                                    atualContorno = getP(info.getY(), info.getX(), bmS);
                                }
                            }while(atualContorno != destino);
                        }
                    }
                }
                escreverMarcadosNaImagemDestino(bmS, bmD);
            }
            imageBitmapSrc.UnlockBits(bmS);
            imageBitmapDest.UnlockBits(bmD);
        }
    }
}
