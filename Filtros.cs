using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace ProcessamentoImagens
{
    class Filtros
    {
        private const int pixelSize = 3;

        public unsafe static byte* getP(int i, int j, BitmapData bd)
        {
            return i>=0 && i<bd.Height && j>=0 && j < bd.Width ? (byte*)bd.Scan0.ToPointer() + (i * bd.Stride) + (j * pixelSize) : null;
        }
        public unsafe static byte* p0(int i, int j, BitmapData bd)
        {
            return getP(i, j + 1, bd);
        }

        public unsafe static byte* p1(int i, int j, BitmapData bd)
        {
            return getP(i - 1, j + 1, bd);
        }

        public unsafe static byte* p2(int i, int j, BitmapData bd)
        {
            return getP(i - 1, j, bd);
        }

        public unsafe static byte* p3(int i, int j, BitmapData bd)
        {
            return getP(i - 1, j - 1, bd);
        }

        public unsafe static byte* p4(int i, int j, BitmapData bd)
        {
            return getP(i, j - 1, bd);
        }

        public unsafe static byte* p5(int i, int j, BitmapData bd)
        {
            return getP(i + 1, j - 1, bd);
        }

        public unsafe static byte* p6(int i, int j, BitmapData bd)
        {
            return getP(i + 1, j, bd);
        }

        public unsafe static byte* p7(int i, int j, BitmapData bd)
        {
            return getP(i + 1, j + 1, bd);
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
                    if (ehPreto(aux)) cor = 0;
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
            return (pixel[0] == 255 && pixel[1] == 255 && pixel[2] == 255);
        }
        private unsafe static bool ehPreto(byte* pixel)
        {
            return pixel[0] == 0 && pixel[1] == 0 && pixel[2] == 0;
        }

        private unsafe static void marcarPixel(byte* pixel)
        {
            pixel[0] = 255; pixel[1] = 0;  pixel[2] = 0;
        }

        public static bool ehConectividade4(int x1, int y1, int x2, int y2){
            return Math.Abs(x1 - x2) + Math.Abs(y1 - y2) == 1;
        }
        public unsafe static void acharPos(int i, int j, int direcao, BitmapData bd, List<int>dir, Pilha p){
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
                        if(ehConectividade4(x, y, j, i)){
                            dir.Insert(0, (idxAnt+4)%8);
                            p.push(new Info(x, y));
                        } else {
                            int posAnt = (idxAnt+7)%8;
                            int posProx = (idxAnt+1)%8;
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
            Pilha p = new Pilha();
            List<int> dir = new List<int>();
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
                                if (!p.isEmpty()) {
                                    Info info = p.pop();
                                    direcao = dir[0];
                                    dir.RemoveAt(0);
                                    iAtual = info.getY();
                                    jAtual = info.getX();
                                    atualContorno = getP(iAtual, jAtual, bmS);
                                } else {
                                    atualContorno = destino;
                                }
                            } while(atualContorno != destino);
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
