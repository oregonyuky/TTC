using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;

namespace ProcessamentoImagens
{
    public partial class frmPrincipal : Form
    {
        private Image image;
        private Bitmap imageBitmap;

        public frmPrincipal()
        {
            InitializeComponent();
        }

        private void btnAbrirImagem_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = "";
            openFileDialog.Filter = "Arquivos de Imagem (*.jpg;*.gif;*.bmp;*.png)|*.jpg;*.gif;*.bmp;*.png";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                image = Image.FromFile(openFileDialog.FileName);
                pictBoxImg1.Image = image;
                pictBoxImg1.SizeMode = PictureBoxSizeMode.Zoom;
                pictBoxImg2.SizeMode = PictureBoxSizeMode.Zoom;
            }
        }

        private void btnLimpar_Click(object sender, EventArgs e)
        {
            pictBoxImg1.Image = null;
            pictBoxImg2.Image = null;
        }

        private void btnLuminanciaSemDMA_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.convert_to_gray(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnLuminanciaComDMA_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.convert_to_grayDMA(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnNegativoSemDMA_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.negativo(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnNegativoComDMA_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.negativoDMA(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnEspelharHorizontal_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.espelharHorizontalDMA(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;

        }

        private void btnEspelharVertical_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.espelharVertical(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnPretoBranco_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.pretoBranco(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnRotacao90_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image.Height, image.Width, PixelFormat.Format24bppRgb);
            imageBitmap = (Bitmap)image;
            Filtros.rotacionar_90(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnInverterVermelhoComAzul_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.inverterVermelhoComAzul(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnSepararRed_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.separarRed(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnSepararGreen_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.separarGreen(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnSepararBlue_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.separarBlue(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnEspelharDiagonal_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image.Height, image.Width, PixelFormat.Format24bppRgb);
            imageBitmap = (Bitmap)image;
            Filtros.espelharDiagonal(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnDividirImagem_Click(object sender, EventArgs e)
        {
            Bitmap imgDest = new Bitmap(image);
            imageBitmap = (Bitmap)image;
            Filtros.dividirImagem(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnSegmentar4_Click(object sender, EventArgs e)
        {
            imageBitmap = (Bitmap)image;
            List<ObjetoSegmentado> objetos;
            Bitmap imgDest = Filtros.segmentar4Conectados(imageBitmap, out objetos);
            pictBoxImg2.Image = imgDest;
        }

        private void btnSegmentar8_Click(object sender, EventArgs e)
        {
            imageBitmap = (Bitmap)image;
            List<ObjetoSegmentado> objetos;
            Bitmap imgDest = Filtros.segmentar8Conectados(imageBitmap, out objetos);
            pictBoxImg2.Image = imgDest;
        }
        private void btnReduzirTamanho_Click(object sender, EventArgs e)
        {
            if (image == null) return;
            imageBitmap = new Bitmap(image);
            Bitmap imgDest = new Bitmap(imageBitmap.Width / 2, imageBitmap.Height / 2, PixelFormat.Format24bppRgb);
            Filtros.ReduzirMetadeDMA(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }

        private void btnReduzirResolucaoCinzaDadoValor_Click(object sender, EventArgs e)
        {
            if (image == null) return;
            const int niveisDeCinza = 8;
            imageBitmap = new Bitmap(image);
            Bitmap imgDest = new Bitmap(imageBitmap.Width, imageBitmap.Height, PixelFormat.Format24bppRgb);
            Filtros.ReduzirEscalaResolucaoCinzaDadoValorDMA(imageBitmap, imgDest, niveisDeCinza);
            pictBoxImg2.Image = imgDest;
        }
        private void btnContourFollowing_Click(object sender, EventArgs e)
        {
            if (image == null) return;
            imageBitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format24bppRgb);
            using (Graphics graphics = Graphics.FromImage(imageBitmap))
            {
                graphics.DrawImage(image, 0, 0, image.Width, image.Height);
            }
            Bitmap imgDest = new Bitmap(imageBitmap);
            Filtros.contourFollowing(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }
    }
}
