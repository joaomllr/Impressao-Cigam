using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CigamPrintTest.Config;

namespace CigamPrintTest
{
    public partial class FormConfiguracaoInicial : Form
    {
        private readonly ConfigApp _configExistente;
        private bool _placeholderAtivo = false;

        public FormConfiguracaoInicial(ConfigApp configExistente = null)
        {
            _configExistente = configExistente;
            InitializeComponent();
            ConfigurarEventos();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (_configExistente != null)
            {
                txtCliente.Text = _configExistente.Cliente ?? string.Empty;
                txtPastaCigam.Text = _configExistente.CigamInstal ?? string.Empty;

                var perfilCigam = _configExistente.Perfis?.FirstOrDefault(p =>
                    string.Equals(p.Nome, "CIGAM - DANFE", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(p.Nome, _configExistente.PerfilPadrao, StringComparison.OrdinalIgnoreCase));

                if (perfilCigam != null && !string.IsNullOrWhiteSpace(perfilCigam.ComandoCigam))
                {
                    txtComandoCigam.Text = perfilCigam.ComandoCigam;
                    _placeholderAtivo = false;
                    txtComandoCigam.ForeColor = SystemColors.WindowText;
                }
            }

            // Sugestão automática para pasta do CIGAM se estiver vazia
            if (string.IsNullOrWhiteSpace(txtPastaCigam.Text))
            {
                var pastaSugerida = CigamComandoHelper.DetectarPastaCigam();
                if (!string.IsNullOrWhiteSpace(pastaSugerida))
                {
                    txtPastaCigam.Text = pastaSugerida;
                }
            }

            // Se o comando ainda estiver vazio, preenche com o valor padrão do CIGAM (LF-NE-2128)
            if (string.IsNullOrWhiteSpace(txtComandoCigam.Text))
            {
                txtComandoCigam.Text = CigamComandoHelper.ExemploConfig2128Padrao;
                txtComandoCigam.ForeColor = SystemColors.WindowText;
                _placeholderAtivo = false;
            }

            ValidarEmTempoReal();
        }

        private void ConfigurarEventos()
        {
            txtPastaCigam.TextChanged += (s, e) => ValidarEmTempoReal();
            txtComandoCigam.TextChanged += (s, e) =>
            {
                if (!_placeholderAtivo)
                {
                    ValidarEmTempoReal();
                }
            };

            txtComandoCigam.GotFocus += (s, e) =>
            {
                if (_placeholderAtivo)
                {
                    txtComandoCigam.Text = string.Empty;
                    txtComandoCigam.ForeColor = SystemColors.WindowText;
                    _placeholderAtivo = false;
                }
            };

            txtComandoCigam.LostFocus += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtComandoCigam.Text))
                {
                    AtivarPlaceholder();
                    ValidarEmTempoReal();
                }
            };

            btnProcurarPasta.Click += (s, e) => ProcurarPastaCigam();
            btnSalvar.Click += (s, e) => SalvarConfiguracao();
            btnCancelar.Click += (s, e) =>
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            };
        }

        private void AtivarPlaceholder()
        {
            _placeholderAtivo = true;
            txtComandoCigam.Text = CigamComandoHelper.ExemploConfig2128Padrao;
            txtComandoCigam.ForeColor = Color.Gray;
        }

        private string ObterValorComandoConfig()
        {
            if (_placeholderAtivo)
            {
                return CigamComandoHelper.ExemploConfig2128Padrao;
            }
            return txtComandoCigam.Text;
        }

        private void ProcurarPastaCigam()
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Selecione a pasta de instalação do CIGAM (onde se localiza o CGEditor.exe):";
                fbd.ShowNewFolderButton = false;

                if (!string.IsNullOrWhiteSpace(txtPastaCigam.Text) && Directory.Exists(txtPastaCigam.Text))
                {
                    fbd.SelectedPath = txtPastaCigam.Text;
                }

                if (fbd.ShowDialog(this) == DialogResult.OK)
                {
                    txtPastaCigam.Text = CigamComandoHelper.NormalizarCigamInstal(fbd.SelectedPath);
                }
            }
        }

        private void ValidarEmTempoReal()
        {
            var pasta = txtPastaCigam.Text.Trim();
            var valorConfig = ObterValorComandoConfig();

            CigamComandoHelper.Resolver(
                valorConfig,
                pasta,
                CigamComandoHelper.ArquivoExemploPreview,
                out var exe,
                out var args,
                out var linhaPreview);

            txtPreviaComando.Text = linhaPreview;

            bool exeExiste = !string.IsNullOrWhiteSpace(exe) && File.Exists(exe);

            if (exeExiste)
            {
                lblStatusCgEditor.Text = $"✓ CGEditor encontrado: {exe}";
                lblStatusCgEditor.ForeColor = Color.FromArgb(0, 130, 0);
                btnSalvar.Enabled = true;
            }
            else
            {
                lblStatusCgEditor.Text = string.IsNullOrWhiteSpace(exe)
                    ? "✗ CGEditor: executável não identificado."
                    : $"✗ CGEditor não encontrado: {exe}";
                lblStatusCgEditor.ForeColor = Color.FromArgb(190, 0, 0);
                btnSalvar.Enabled = false;
            }
        }

        private void SalvarConfiguracao()
        {
            var pasta = CigamComandoHelper.NormalizarCigamInstal(txtPastaCigam.Text);
            var valorConfig = ObterValorComandoConfig().Trim();
            var cliente = txtCliente.Text.Trim();

            // Clona config existente ou gera nova
            var config = _configExistente ?? new ConfigApp();
            config.Configurado = true;
            config.Cliente = cliente;
            config.CigamInstal = pasta;
            config.PerfilPadrao = "CIGAM - DANFE";

            if (config.Perfis == null)
            {
                config.Perfis = new List<PerfilImpressao>();
            }

            // 1. Perfil CIGAM - DANFE com comandoCigam
            var perfilDanfe = config.Perfis.FirstOrDefault(p =>
                string.Equals(p.Nome, "CIGAM - DANFE", StringComparison.OrdinalIgnoreCase));

            if (perfilDanfe == null)
            {
                perfilDanfe = new PerfilImpressao
                {
                    Nome = "CIGAM - DANFE",
                    PastaExecucao = string.Empty,
                    EsperarProcesso = false,
                    TimeoutProcessoMs = 60000,
                    ExtensoesPermitidas = new List<string> { ".rtf" },
                    PdfEsperado = string.Empty
                };
                config.Perfis.Insert(0, perfilDanfe);
            }

            perfilDanfe.ComandoCigam = valorConfig;
            perfilDanfe.CigamInstal = pasta;
            perfilDanfe.EsperarProcesso = false;
            if (perfilDanfe.ExtensoesPermitidas == null || perfilDanfe.ExtensoesPermitidas.Count == 0)
            {
                perfilDanfe.ExtensoesPermitidas = new List<string> { ".rtf" };
            }

            // Remove perfis CIGAM antigos (ex.: "CIGAM - DANFE (CGEditor)" com caminho fixo de outro cliente)
            config.Perfis.RemoveAll(p => !ReferenceEquals(p, perfilDanfe) &&
                p.Nome != null && p.Nome.StartsWith("CIGAM - DANFE", StringComparison.OrdinalIgnoreCase));

            // 2. Perfil Teste do programa (Notepad), com esperarProcesso=false
            var perfilNotepad = config.Perfis.FirstOrDefault(p =>
                string.Equals(p.Nome, "Teste do programa (Notepad)", StringComparison.OrdinalIgnoreCase));

            if (perfilNotepad == null)
            {
                perfilNotepad = new PerfilImpressao
                {
                    Nome = "Teste do programa (Notepad)",
                    Executavel = @"C:\Windows\System32\notepad.exe",
                    Argumentos = "/p {arquivo}",
                    PastaExecucao = string.Empty,
                    EsperarProcesso = false,
                    TimeoutProcessoMs = 60000,
                    ExtensoesPermitidas = new List<string> { ".txt" },
                    PdfEsperado = string.Empty
                };
                config.Perfis.Add(perfilNotepad);
            }
            else
            {
                perfilNotepad.EsperarProcesso = false;
            }

            var caminhoDestino = ConfigLoader.ObterCaminhoPadrao();
            var pastaDestino = Path.GetDirectoryName(caminhoDestino);

            try
            {
                ConfigLoader.Salvar(config, caminhoDestino);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show(
                    $"Sem permissão para gravar em {pastaDestino}. Execute a configuração inicial uma vez com um usuário que tenha permissão de escrita nessa pasta.",
                    "Permissão de Gravação",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                if (ex is System.Security.SecurityException)
                {
                    MessageBox.Show(
                        $"Sem permissão para gravar em {pastaDestino}. Execute a configuração inicial uma vez com um usuário que tenha permissão de escrita nessa pasta.",
                        "Permissão de Gravação",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(
                    $"Erro ao salvar configuração em '{caminhoDestino}':\n\n{ex.Message}",
                    "Erro ao Salvar",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }
    }
}
