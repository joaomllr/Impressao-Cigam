namespace CigamPrintTest
{
    partial class FormConfiguracaoInicial
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label lblTitulo;
        private System.Windows.Forms.Label lblDescricao;
        private System.Windows.Forms.Label lblCliente;
        private System.Windows.Forms.TextBox txtCliente;
        private System.Windows.Forms.Label lblPastaCigam;
        private System.Windows.Forms.TextBox txtPastaCigam;
        private System.Windows.Forms.Button btnProcurarPasta;
        private System.Windows.Forms.Label lblComandoCigam;
        private System.Windows.Forms.TextBox txtComandoCigam;
        private System.Windows.Forms.GroupBox grpValidacao;
        private System.Windows.Forms.Label lblStatusCgEditor;
        private System.Windows.Forms.Label lblPreviaTitulo;
        private System.Windows.Forms.TextBox txtPreviaComando;
        private System.Windows.Forms.Button btnSalvar;
        private System.Windows.Forms.Button btnCancelar;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitulo = new System.Windows.Forms.Label();
            this.lblDescricao = new System.Windows.Forms.Label();
            this.lblCliente = new System.Windows.Forms.Label();
            this.txtCliente = new System.Windows.Forms.TextBox();
            this.lblPastaCigam = new System.Windows.Forms.Label();
            this.txtPastaCigam = new System.Windows.Forms.TextBox();
            this.btnProcurarPasta = new System.Windows.Forms.Button();
            this.lblComandoCigam = new System.Windows.Forms.Label();
            this.txtComandoCigam = new System.Windows.Forms.TextBox();
            this.grpValidacao = new System.Windows.Forms.GroupBox();
            this.lblStatusCgEditor = new System.Windows.Forms.Label();
            this.lblPreviaTitulo = new System.Windows.Forms.Label();
            this.txtPreviaComando = new System.Windows.Forms.TextBox();
            this.btnSalvar = new System.Windows.Forms.Button();
            this.btnCancelar = new System.Windows.Forms.Button();
            this.grpValidacao.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblTitulo
            // 
            this.lblTitulo.AutoSize = true;
            this.lblTitulo.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitulo.ForeColor = System.Drawing.Color.FromArgb(20, 60, 110);
            this.lblTitulo.Location = new System.Drawing.Point(18, 16);
            this.lblTitulo.Name = "lblTitulo";
            this.lblTitulo.Size = new System.Drawing.Size(434, 28);
            this.lblTitulo.TabIndex = 0;
            this.lblTitulo.Text = "Assistente de Configuração Inicial do CIGAM";
            // 
            // lblDescricao
            // 
            this.lblDescricao.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblDescricao.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80);
            this.lblDescricao.Location = new System.Drawing.Point(20, 48);
            this.lblDescricao.Name = "lblDescricao";
            this.lblDescricao.Size = new System.Drawing.Size(650, 40);
            this.lblDescricao.TabIndex = 1;
            this.lblDescricao.Text = "Configure o ambiente do cliente para permitir a reprodução exata da chamada do visualizador DANFE (CGEditor) pelo ERP CIGAM.";
            // 
            // lblCliente
            // 
            this.lblCliente.AutoSize = true;
            this.lblCliente.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblCliente.Location = new System.Drawing.Point(20, 96);
            this.lblCliente.Name = "lblCliente";
            this.lblCliente.Size = new System.Drawing.Size(63, 20);
            this.lblCliente.TabIndex = 2;
            this.lblCliente.Text = "Cliente:";
            // 
            // txtCliente
            // 
            this.txtCliente.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtCliente.Location = new System.Drawing.Point(23, 120);
            this.txtCliente.Name = "txtCliente";
            this.txtCliente.Size = new System.Drawing.Size(645, 29);
            this.txtCliente.TabIndex = 3;
            // 
            // lblPastaCigam
            // 
            this.lblPastaCigam.AutoSize = true;
            this.lblPastaCigam.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblPastaCigam.Location = new System.Drawing.Point(20, 160);
            this.lblPastaCigam.Name = "lblPastaCigam";
            this.lblPastaCigam.Size = new System.Drawing.Size(329, 20);
            this.lblPastaCigam.TabIndex = 4;
            this.lblPastaCigam.Text = "Pasta de instalação do CIGAM (%CIGAM_INSTAL%):";
            // 
            // txtPastaCigam
            // 
            this.txtPastaCigam.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtPastaCigam.Location = new System.Drawing.Point(23, 184);
            this.txtPastaCigam.Name = "txtPastaCigam";
            this.txtPastaCigam.Size = new System.Drawing.Size(535, 29);
            this.txtPastaCigam.TabIndex = 5;
            // 
            // btnProcurarPasta
            // 
            this.btnProcurarPasta.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnProcurarPasta.Location = new System.Drawing.Point(566, 183);
            this.btnProcurarPasta.Name = "btnProcurarPasta";
            this.btnProcurarPasta.Size = new System.Drawing.Size(102, 31);
            this.btnProcurarPasta.TabIndex = 6;
            this.btnProcurarPasta.Text = "Procurar...";
            this.btnProcurarPasta.UseVisualStyleBackColor = true;
            // 
            // lblComandoCigam
            // 
            this.lblComandoCigam.AutoSize = true;
            this.lblComandoCigam.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblComandoCigam.Location = new System.Drawing.Point(20, 226);
            this.lblComandoCigam.Name = "lblComandoCigam";
            this.lblComandoCigam.Size = new System.Drawing.Size(332, 20);
            this.lblComandoCigam.TabIndex = 7;
            this.lblComandoCigam.Text = "Valor da config LF-NE-2128 (copie do CIGAM):";
            // 
            // txtComandoCigam
            // 
            this.txtComandoCigam.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.txtComandoCigam.Location = new System.Drawing.Point(23, 250);
            this.txtComandoCigam.Name = "txtComandoCigam";
            this.txtComandoCigam.Size = new System.Drawing.Size(645, 29);
            this.txtComandoCigam.TabIndex = 8;
            // 
            // grpValidacao
            // 
            this.grpValidacao.Controls.Add(this.lblStatusCgEditor);
            this.grpValidacao.Controls.Add(this.lblPreviaTitulo);
            this.grpValidacao.Controls.Add(this.txtPreviaComando);
            this.grpValidacao.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.grpValidacao.Location = new System.Drawing.Point(23, 292);
            this.grpValidacao.Name = "grpValidacao";
            this.grpValidacao.Size = new System.Drawing.Size(645, 168);
            this.grpValidacao.TabIndex = 9;
            this.grpValidacao.TabStop = false;
            this.grpValidacao.Text = "Validação em Tempo Real";
            // 
            // lblStatusCgEditor
            // 
            this.lblStatusCgEditor.AutoEllipsis = true;
            this.lblStatusCgEditor.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblStatusCgEditor.Location = new System.Drawing.Point(12, 25);
            this.lblStatusCgEditor.Name = "lblStatusCgEditor";
            this.lblStatusCgEditor.Size = new System.Drawing.Size(620, 26);
            this.lblStatusCgEditor.TabIndex = 0;
            this.lblStatusCgEditor.Text = "CGEditor: verificando...";
            // 
            // lblPreviaTitulo
            // 
            this.lblPreviaTitulo.AutoSize = true;
            this.lblPreviaTitulo.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);
            this.lblPreviaTitulo.ForeColor = System.Drawing.Color.FromArgb(90, 90, 90);
            this.lblPreviaTitulo.Location = new System.Drawing.Point(12, 58);
            this.lblPreviaTitulo.Name = "lblPreviaTitulo";
            this.lblPreviaTitulo.Size = new System.Drawing.Size(370, 20);
            this.lblPreviaTitulo.TabIndex = 1;
            this.lblPreviaTitulo.Text = @"Prévia do comando (C:\exemplo\DANFE30000044312.rtf):";
            // 
            // txtPreviaComando
            // 
            this.txtPreviaComando.BackColor = System.Drawing.Color.FromArgb(245, 247, 250);
            this.txtPreviaComando.Font = new System.Drawing.Font("Consolas", 8.5F);
            this.txtPreviaComando.Location = new System.Drawing.Point(12, 82);
            this.txtPreviaComando.Multiline = true;
            this.txtPreviaComando.Name = "txtPreviaComando";
            this.txtPreviaComando.ReadOnly = true;
            this.txtPreviaComando.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtPreviaComando.Size = new System.Drawing.Size(620, 72);
            this.txtPreviaComando.TabIndex = 2;
            // 
            // btnSalvar
            // 
            this.btnSalvar.BackColor = System.Drawing.Color.FromArgb(20, 110, 190);
            this.btnSalvar.Enabled = false;
            this.btnSalvar.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnSalvar.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.btnSalvar.Location = new System.Drawing.Point(448, 474);
            this.btnSalvar.Name = "btnSalvar";
            this.btnSalvar.Size = new System.Drawing.Size(108, 36);
            this.btnSalvar.TabIndex = 10;
            this.btnSalvar.Text = "Salvar";
            this.btnSalvar.UseVisualStyleBackColor = true;
            // 
            // btnCancelar
            // 
            this.btnCancelar.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.btnCancelar.Location = new System.Drawing.Point(566, 474);
            this.btnCancelar.Name = "btnCancelar";
            this.btnCancelar.Size = new System.Drawing.Size(102, 36);
            this.btnCancelar.TabIndex = 11;
            this.btnCancelar.Text = "Cancelar";
            this.btnCancelar.UseVisualStyleBackColor = true;
            // 
            // FormConfiguracaoInicial
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(692, 526);
            this.Controls.Add(this.btnCancelar);
            this.Controls.Add(this.btnSalvar);
            this.Controls.Add(this.grpValidacao);
            this.Controls.Add(this.txtComandoCigam);
            this.Controls.Add(this.lblComandoCigam);
            this.Controls.Add(this.btnProcurarPasta);
            this.Controls.Add(this.txtPastaCigam);
            this.Controls.Add(this.lblPastaCigam);
            this.Controls.Add(this.txtCliente);
            this.Controls.Add(this.lblCliente);
            this.Controls.Add(this.lblDescricao);
            this.Controls.Add(this.lblTitulo);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormConfiguracaoInicial";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuração Inicial do CIGAM - CigamPrintTest";
            this.grpValidacao.ResumeLayout(false);
            this.grpValidacao.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
