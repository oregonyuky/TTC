namespace ProcessamentoImagens
{
    partial class frmPrincipal
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pictBoxImg1 = new System.Windows.Forms.PictureBox();
            this.pictBoxImg2 = new System.Windows.Forms.PictureBox();
            this.btnAbrirImagem = new System.Windows.Forms.Button();
            this.btnLimpar = new System.Windows.Forms.Button();
            this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.btnLuminanciaSemDMA = new System.Windows.Forms.Button();
            this.btnLuminanciaComDMA = new System.Windows.Forms.Button();
            this.btnNegativoComDMA = new System.Windows.Forms.Button();
            this.btnNegativoSemDMA = new System.Windows.Forms.Button();
            this.btnEspelharHorizontal = new System.Windows.Forms.Button();
            this.btnEspelharVertical = new System.Windows.Forms.Button();
            this.btnPretoBranco = new System.Windows.Forms.Button();
            this.btnRotacao90 = new System.Windows.Forms.Button();
            this.btnInverterVermelhoComAzul = new System.Windows.Forms.Button();
            this.btnSepararRed = new System.Windows.Forms.Button();
            this.btnEspelharDiagonal = new System.Windows.Forms.Button();
            this.btnDividirImagem = new System.Windows.Forms.Button();
            this.btnSepararGreen = new System.Windows.Forms.Button();
            this.btnSepararBlue = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.pictBoxImg1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictBoxImg2)).BeginInit();
            this.SuspendLayout();
            // 
            // pictBoxImg1
            // 
            this.pictBoxImg1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictBoxImg1.Location = new System.Drawing.Point(5, 6);
            this.pictBoxImg1.Name = "pictBoxImg1";
            this.pictBoxImg1.Size = new System.Drawing.Size(600, 462);
            this.pictBoxImg1.TabIndex = 102;
            this.pictBoxImg1.TabStop = false;
            // 
            // pictBoxImg2
            // 
            this.pictBoxImg2.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.pictBoxImg2.Location = new System.Drawing.Point(611, 6);
            this.pictBoxImg2.Name = "pictBoxImg2";
            this.pictBoxImg2.Size = new System.Drawing.Size(600, 462);
            this.pictBoxImg2.TabIndex = 105;
            this.pictBoxImg2.TabStop = false;
            // 
            // btnAbrirImagem
            // 
            this.btnAbrirImagem.Location = new System.Drawing.Point(5, 473);
            this.btnAbrirImagem.Name = "btnAbrirImagem";
            this.btnAbrirImagem.Size = new System.Drawing.Size(101, 21);
            this.btnAbrirImagem.TabIndex = 106;
            this.btnAbrirImagem.Text = "Abrir Imagem";
            this.btnAbrirImagem.UseVisualStyleBackColor = true;
            this.btnAbrirImagem.Click += new System.EventHandler(this.btnAbrirImagem_Click);
            // 
            // btnLimpar
            // 
            this.btnLimpar.Location = new System.Drawing.Point(5, 501);
            this.btnLimpar.Name = "btnLimpar";
            this.btnLimpar.Size = new System.Drawing.Size(101, 21);
            this.btnLimpar.TabIndex = 107;
            this.btnLimpar.Text = "Limpar";
            this.btnLimpar.UseVisualStyleBackColor = true;
            this.btnLimpar.Click += new System.EventHandler(this.btnLimpar_Click);
            // 
            // btnLuminanciaSemDMA
            // 
            this.btnLuminanciaSemDMA.Location = new System.Drawing.Point(166, 475);
            this.btnLuminanciaSemDMA.Name = "btnLuminanciaSemDMA";
            this.btnLuminanciaSemDMA.Size = new System.Drawing.Size(151, 21);
            this.btnLuminanciaSemDMA.TabIndex = 108;
            this.btnLuminanciaSemDMA.Text = "Luminância sem DMA";
            this.btnLuminanciaSemDMA.UseVisualStyleBackColor = true;
            this.btnLuminanciaSemDMA.Click += new System.EventHandler(this.btnLuminanciaSemDMA_Click);
            // 
            // btnLuminanciaComDMA
            // 
            this.btnLuminanciaComDMA.Location = new System.Drawing.Point(166, 500);
            this.btnLuminanciaComDMA.Name = "btnLuminanciaComDMA";
            this.btnLuminanciaComDMA.Size = new System.Drawing.Size(151, 21);
            this.btnLuminanciaComDMA.TabIndex = 109;
            this.btnLuminanciaComDMA.Text = "Luminância com DMA";
            this.btnLuminanciaComDMA.UseVisualStyleBackColor = true;
            this.btnLuminanciaComDMA.Click += new System.EventHandler(this.btnLuminanciaComDMA_Click);
            // 
            // btnNegativoComDMA
            // 
            this.btnNegativoComDMA.Location = new System.Drawing.Point(323, 500);
            this.btnNegativoComDMA.Name = "btnNegativoComDMA";
            this.btnNegativoComDMA.Size = new System.Drawing.Size(125, 21);
            this.btnNegativoComDMA.TabIndex = 111;
            this.btnNegativoComDMA.Text = "Negativo com DMA";
            this.btnNegativoComDMA.UseVisualStyleBackColor = true;
            this.btnNegativoComDMA.Click += new System.EventHandler(this.btnNegativoComDMA_Click);
            // 
            // btnNegativoSemDMA
            // 
            this.btnNegativoSemDMA.Location = new System.Drawing.Point(323, 475);
            this.btnNegativoSemDMA.Name = "btnNegativoSemDMA";
            this.btnNegativoSemDMA.Size = new System.Drawing.Size(125, 21);
            this.btnNegativoSemDMA.TabIndex = 110;
            this.btnNegativoSemDMA.Text = "Negativo sem DMA";
            this.btnNegativoSemDMA.UseVisualStyleBackColor = true;
            this.btnNegativoSemDMA.Click += new System.EventHandler(this.btnNegativoSemDMA_Click);
            // 
            // btnEspelharHorizontal
            // 
            this.btnEspelharHorizontal.Location = new System.Drawing.Point(454, 475);
            this.btnEspelharHorizontal.Name = "btnEspelharHorizontal";
            this.btnEspelharHorizontal.Size = new System.Drawing.Size(125, 21);
            this.btnEspelharHorizontal.TabIndex = 112;
            this.btnEspelharHorizontal.Text = "Espelhar Horizontal";
            this.btnEspelharHorizontal.UseVisualStyleBackColor = true;
            this.btnEspelharHorizontal.Click += new System.EventHandler(this.btnEspelharHorizontal_Click);
            // 
            // btnEspelharVertical
            // 
            this.btnEspelharVertical.Location = new System.Drawing.Point(454, 500);
            this.btnEspelharVertical.Name = "btnEspelharVertical";
            this.btnEspelharVertical.Size = new System.Drawing.Size(125, 21);
            this.btnEspelharVertical.TabIndex = 113;
            this.btnEspelharVertical.Text = "Espelhar Vertical";
            this.btnEspelharVertical.UseVisualStyleBackColor = true;
            this.btnEspelharVertical.Click += new System.EventHandler(this.btnEspelharVertical_Click);
            // 
            // btnPretoBranco
            // 
            this.btnPretoBranco.Location = new System.Drawing.Point(585, 475);
            this.btnPretoBranco.Name = "btnPretoBranco";
            this.btnPretoBranco.Size = new System.Drawing.Size(141, 21);
            this.btnPretoBranco.TabIndex = 114;
            this.btnPretoBranco.Text = "Preto e branco";
            this.btnPretoBranco.UseVisualStyleBackColor = true;
            this.btnPretoBranco.Click += new System.EventHandler(this.btnPretoBranco_Click);
            // 
            // btnRotacao90
            // 
            this.btnRotacao90.Location = new System.Drawing.Point(585, 500);
            this.btnRotacao90.Name = "btnRotacao90";
            this.btnRotacao90.Size = new System.Drawing.Size(141, 21);
            this.btnRotacao90.TabIndex = 115;
            this.btnRotacao90.Text = "Rotacionar 90 graus";
            this.btnRotacao90.UseVisualStyleBackColor = true;
            this.btnRotacao90.Click += new System.EventHandler(this.btnRotacao90_Click);
            // 
            // btnInverterVermelhoComAzul
            // 
            this.btnInverterVermelhoComAzul.Location = new System.Drawing.Point(732, 474);
            this.btnInverterVermelhoComAzul.Name = "btnInverterVermelhoComAzul";
            this.btnInverterVermelhoComAzul.Size = new System.Drawing.Size(182, 21);
            this.btnInverterVermelhoComAzul.TabIndex = 116;
            this.btnInverterVermelhoComAzul.Text = "Inverter Vermelho Com Azul";
            this.btnInverterVermelhoComAzul.UseVisualStyleBackColor = true;
            this.btnInverterVermelhoComAzul.Click += new System.EventHandler(this.btnInverterVermelhoComAzul_Click);
            // 
            // btnSepararRed
            // 
            this.btnSepararRed.Location = new System.Drawing.Point(732, 501);
            this.btnSepararRed.Name = "btnSepararRed";
            this.btnSepararRed.Size = new System.Drawing.Size(182, 21);
            this.btnSepararRed.TabIndex = 117;
            this.btnSepararRed.Text = "Separar Red";
            this.btnSepararRed.UseVisualStyleBackColor = true;
            this.btnSepararRed.Click += new System.EventHandler(this.btnSepararRed_Click);
            // 
            // btnEspelharDiagonal
            // 
            this.btnEspelharDiagonal.Location = new System.Drawing.Point(1042, 475);
            this.btnEspelharDiagonal.Name = "btnEspelharDiagonal";
            this.btnEspelharDiagonal.Size = new System.Drawing.Size(116, 20);
            this.btnEspelharDiagonal.TabIndex = 118;
            this.btnEspelharDiagonal.Text = "Espelhar Diagonal";
            this.btnEspelharDiagonal.UseVisualStyleBackColor = true;
            this.btnEspelharDiagonal.Click += new System.EventHandler(this.btnEspelharDiagonal_Click);
            // 
            // btnDividirImagem
            // 
            this.btnDividirImagem.Location = new System.Drawing.Point(1042, 501);
            this.btnDividirImagem.Name = "btnDividirImagem";
            this.btnDividirImagem.Size = new System.Drawing.Size(116, 21);
            this.btnDividirImagem.TabIndex = 119;
            this.btnDividirImagem.Text = "Dividir a Imagem";
            this.btnDividirImagem.UseVisualStyleBackColor = true;
            this.btnDividirImagem.Click += new System.EventHandler(this.btnDividirImagem_Click);
            // 
            // btnSepararGreen
            // 
            this.btnSepararGreen.Location = new System.Drawing.Point(920, 474);
            this.btnSepararGreen.Name = "btnSepararGreen";
            this.btnSepararGreen.Size = new System.Drawing.Size(116, 20);
            this.btnSepararGreen.TabIndex = 120;
            this.btnSepararGreen.Text = "Separar Green";
            this.btnSepararGreen.UseVisualStyleBackColor = true;
            this.btnSepararGreen.Click += new System.EventHandler(this.btnSepararGreen_Click);
            // 
            // btnSepararBlue
            // 
            this.btnSepararBlue.Location = new System.Drawing.Point(920, 502);
            this.btnSepararBlue.Name = "btnSepararBlue";
            this.btnSepararBlue.Size = new System.Drawing.Size(116, 20);
            this.btnSepararBlue.TabIndex = 121;
            this.btnSepararBlue.Text = "Separar Blue";
            this.btnSepararBlue.UseVisualStyleBackColor = true;
            this.btnSepararBlue.Click += new System.EventHandler(this.btnSepararBlue_Click);
            // 
            // frmPrincipal
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1220, 561);
            this.Controls.Add(this.btnSepararBlue);
            this.Controls.Add(this.btnSepararGreen);
            this.Controls.Add(this.btnDividirImagem);
            this.Controls.Add(this.btnEspelharDiagonal);
            this.Controls.Add(this.btnSepararRed);
            this.Controls.Add(this.btnInverterVermelhoComAzul);
            this.Controls.Add(this.btnRotacao90);
            this.Controls.Add(this.btnPretoBranco);
            this.Controls.Add(this.btnEspelharVertical);
            this.Controls.Add(this.btnEspelharHorizontal);
            this.Controls.Add(this.btnNegativoComDMA);
            this.Controls.Add(this.btnNegativoSemDMA);
            this.Controls.Add(this.btnLuminanciaComDMA);
            this.Controls.Add(this.btnLuminanciaSemDMA);
            this.Controls.Add(this.btnLimpar);
            this.Controls.Add(this.btnAbrirImagem);
            this.Controls.Add(this.pictBoxImg2);
            this.Controls.Add(this.pictBoxImg1);
            this.Name = "frmPrincipal";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Formulário Principal";
            ((System.ComponentModel.ISupportInitialize)(this.pictBoxImg1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictBoxImg2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictBoxImg1;
        private System.Windows.Forms.PictureBox pictBoxImg2;
        private System.Windows.Forms.Button btnAbrirImagem;
        private System.Windows.Forms.Button btnLimpar;
        private System.Windows.Forms.OpenFileDialog openFileDialog;
        private System.Windows.Forms.Button btnLuminanciaSemDMA;
        private System.Windows.Forms.Button btnLuminanciaComDMA;
        private System.Windows.Forms.Button btnNegativoComDMA;
        private System.Windows.Forms.Button btnNegativoSemDMA;
        private System.Windows.Forms.Button btnEspelharHorizontal;
        private System.Windows.Forms.Button btnEspelharVertical;
        private System.Windows.Forms.Button btnPretoBranco;
        private System.Windows.Forms.Button btnRotacao90;
        private System.Windows.Forms.Button btnInverterVermelhoComAzul;
        private System.Windows.Forms.Button btnSepararRed;
        private System.Windows.Forms.Button btnEspelharDiagonal;
        private System.Windows.Forms.Button btnDividirImagem;
        private System.Windows.Forms.Button btnSepararGreen;
        private System.Windows.Forms.Button btnSepararBlue;
    }
}

