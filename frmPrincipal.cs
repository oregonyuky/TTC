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
            image = null;
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
            Filtros.contourFollowingDMA(imageBitmap, imgDest);
            pictBoxImg2.Image = imgDest;
        }
    }
}
