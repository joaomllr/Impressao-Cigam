using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CigamPrintTest.Config;
using CigamPrintTest.Diagnostico;
using CigamPrintTest.Erros;
using CigamPrintTest.Execucao;
using CigamPrintTest.Fila;
using CigamPrintTest.Log;
using CigamPrintTest.Modelo;

namespace CigamPrintTest
{
    public partial class MainForm : Form
    {
        // Parâmetros de inicialização via CLI
        private readonly string _argArquivo;
        private readonly string _argPerfil;
        private readonly int? _argVias;
        private readonly bool _argAuto;

        // Estado da aplicação
        private ConfigApp _config;
        private IFonteFilas _fonteFilas;
        private ResultadoTeste _ultimoResultado;
        private string _ultimaPastaSelecionada = string.Empty;
        private bool _emExecucao = false;

        // Controles de UI criados programaticamente
        private ComboBox _cboPerfil;
        private TextBox _txtArquivo;
        private Button _btnAbrirArquivo;
        private NumericUpDown _numVias;
        private Button _btnDiagAmbiente;
        private Button _btnImprimirCigam;
        private DataGridView _gridEtapas;
        private DataGridView _gridImpressoras;
        private TextBox _txtLogTempoReal;
        private TextBox _txtDiagnosticoFinal;
        private Button _btnPapelSim;
        private Button _btnPapelNao;
        private Button _btnPapelNaoSei;
        private Button _btnCopiarDiag;
        private Button _btnAbrirLogs;
        private Label _lblBannerSessao;

        public MainForm(string arquivo = null, string perfil = null, int? vias = null, bool auto = false)
        {
            _argArquivo = arquivo;
            _argPerfil = perfil;
            _argVias = vias;
            _argAuto = auto;

            InitializeComponent();
            ConstruirLayoutInterface();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                _fonteFilas = new FonteFilasSpooler();
                CarregarConfiguracaoOuOferecerPadrao();
                AtualizarBannerSessao();
                AtualizarGridImpressoras();
                AplicarArgumentosLinhaDeComando();
            }
            catch (Exception ex)
            {
                RegistrarLogTempoReal($"Erro na inicialização: {ex.Message}");
                MessageBox.Show(
                    $"Erro durante a inicialização do aplicativo:\n\n{ex.Message}",
                    "Aviso de Inicialização",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (_argAuto && !_emExecucao)
            {
                RegistrarLogTempoReal("Modo --auto detectado na linha de comando. Iniciando teste automaticamente...");
                IniciarTeste(false);
            }
        }

        #region Construção do Layout

        private void ConstruirLayoutInterface()
        {
            this.Text = "Diagnóstico de Impressão ERP CIGAM (Spooler, CGEditor & RDP)";
            this.Size = new Size(1080, 820);
            this.StartPosition = FormStartPosition.CenterScreen;

            var painelPrincipal = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(12)
            };
            painelPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Banner
            painelPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 125F)); // Parâmetros
            painelPrincipal.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));  // Grade de Etapas
            painelPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 130F)); // Confirmação e Diagnóstico
            painelPrincipal.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));  // Rodapé botões
            this.Controls.Add(painelPrincipal);

            // 1. Banner Superior
            _lblBannerSessao = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                BackColor = Color.FromArgb(235, 243, 250),
                ForeColor = Color.FromArgb(20, 60, 110),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "Carregando informações da sessão Windows..."
            };
            painelPrincipal.Controls.Add(_lblBannerSessao, 0, 0);

            // 2. Grupo de Parâmetros
            var grpParametros = new GroupBox
            {
                Text = "Parâmetros de Teste",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            painelPrincipal.Controls.Add(grpParametros, 0, 1);

            var tblParametros = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 6,
                RowCount = 2,
                Padding = new Padding(8, 4, 8, 4)
            };
            tblParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F)); // Labels
            tblParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));  // Combo / TextBox
            tblParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F)); // Botão Abrir
            tblParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60F));  // Vias label
            tblParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 70F));  // Vias numeric
            tblParametros.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));  // Botões ação

            // Linha 1: Perfil e Botão Ambiente
            var lblPerfil = new Label
            {
                Text = "Perfil de Impressão:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _cboPerfil = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _cboPerfil.SelectedIndexChanged += (s, e) => AtualizarFiltroArquivo();

            _btnDiagAmbiente = new Button
            {
                Text = "Diagnóstico do Ambiente",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = Color.FromArgb(240, 240, 245)
            };
            _btnDiagAmbiente.Click += (s, e) => IniciarTeste(true);

            tblParametros.Controls.Add(lblPerfil, 0, 0);
            tblParametros.Controls.Add(_cboPerfil, 1, 0);
            tblParametros.SetColumnSpan(_cboPerfil, 4);
            tblParametros.Controls.Add(_btnDiagAmbiente, 5, 0);

            // Linha 2: Arquivo, Vias e Botão Imprimir CIGAM
            var lblArquivo = new Label
            {
                Text = "Arquivo RTF / Teste:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _txtArquivo = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _btnAbrirArquivo = new Button
            {
                Text = "Abrir arquivo...",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _btnAbrirArquivo.Click += (s, e) => SelecionarArquivo();

            var lblVias = new Label
            {
                Text = "Vias:",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _numVias = new NumericUpDown
            {
                Dock = DockStyle.Fill,
                Minimum = 1,
                Maximum = 10,
                Value = 1,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            _btnImprimirCigam = new Button
            {
                Text = "Imprimir pelo caminho do CIGAM",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(30, 130, 60),
                ForeColor = Color.White
            };
            _btnImprimirCigam.Click += (s, e) => IniciarTeste(false);

            tblParametros.Controls.Add(lblArquivo, 0, 1);
            tblParametros.Controls.Add(_txtArquivo, 1, 1);
            tblParametros.Controls.Add(_btnAbrirArquivo, 2, 1);
            tblParametros.Controls.Add(lblVias, 3, 1);
            tblParametros.Controls.Add(_numVias, 4, 1);
            tblParametros.Controls.Add(_btnImprimirCigam, 5, 1);

            grpParametros.Controls.Add(tblParametros);

            // 3. Área Central (Abas: Checklist de Etapas / Impressoras / Log)
            var tabCentral = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            painelPrincipal.Controls.Add(tabCentral, 0, 2);

            // Aba 1: Etapas
            var tabEtapas = new TabPage("Checklist de Diagnóstico (Etapas 1 a 9)");
            _gridEtapas = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells
            };
            ConfigurarGridEtapas();
            tabEtapas.Controls.Add(_gridEtapas);
            tabCentral.TabPages.Add(tabEtapas);

            // Aba 2: Impressoras nesta sessão
            var tabImpressoras = new TabPage("Impressoras nesta Sessão");
            var pnlImpressoras = new Panel { Dock = DockStyle.Fill };
            var pnlTopoImpressoras = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 36,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(4)
            };
            var btnAtualizarImpressoras = new Button
            {
                Text = "Atualizar Lista de Impressoras",
                Width = 200,
                Height = 28
            };
            btnAtualizarImpressoras.Click += (s, e) => AtualizarGridImpressoras();
            pnlTopoImpressoras.Controls.Add(btnAtualizarImpressoras);

            _gridImpressoras = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None
            };
            ConfigurarGridImpressoras();

            pnlImpressoras.Controls.Add(_gridImpressoras);
            pnlImpressoras.Controls.Add(pnlTopoImpressoras);
            tabImpressoras.Controls.Add(pnlImpressoras);
            tabCentral.TabPages.Add(tabImpressoras);

            // Aba 3: Log em tempo real
            var tabLog = new TabPage("Log de Execução em Tempo Real");
            _txtLogTempoReal = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                BackColor = Color.FromArgb(250, 250, 250),
                Font = new Font("Consolas", 8.5F)
            };
            tabLog.Controls.Add(_txtLogTempoReal);
            tabCentral.TabPages.Add(tabLog);

            // 4. Confirmação do Usuário e Diagnóstico Final
            var pnlDiagnosticoEConfirmacao = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            pnlDiagnosticoEConfirmacao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            pnlDiagnosticoEConfirmacao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            painelPrincipal.Controls.Add(pnlDiagnosticoEConfirmacao, 0, 3);

            // Painel Confirmação
            var grpConfirmacao = new GroupBox
            {
                Text = "Etapa 9: Confirmação Visual",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            var pnlBotoesConfirmacao = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 38,
                FlowDirection = FlowDirection.LeftToRight
            };

            var lblPergunta = new Label
            {
                Text = "O documento saiu no papel?",
                AutoSize = true,
                Padding = new Padding(0, 6, 8, 0),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _btnPapelSim = new Button { Text = "Sim", Width = 65, Height = 28, Enabled = false, BackColor = Color.FromArgb(230, 245, 230) };
            _btnPapelNao = new Button { Text = "Não", Width = 65, Height = 28, Enabled = false, BackColor = Color.FromArgb(255, 235, 235) };
            _btnPapelNaoSei = new Button { Text = "Não sei", Width = 75, Height = 28, Enabled = false };

            _btnPapelSim.Click += (s, e) => RegistrarRespostaPapel("Sim");
            _btnPapelNao.Click += (s, e) => RegistrarRespostaPapel("Não");
            _btnPapelNaoSei.Click += (s, e) => RegistrarRespostaPapel("Não sei");

            pnlBotoesConfirmacao.Controls.Add(lblPergunta);
            pnlBotoesConfirmacao.Controls.Add(_btnPapelSim);
            pnlBotoesConfirmacao.Controls.Add(_btnPapelNao);
            pnlBotoesConfirmacao.Controls.Add(_btnPapelNaoSei);

            var lblNotaSpooler = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 7.8F, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Text = "Atenção: O status 'entregue à impressora' indica que o Spooler enviou os dados para a porta/driver. Em portas TCP/IP sem SNMP ou impressoras redirecionadas de RDP, o Windows sempre reporta 'pronta' mesmo com erro físico (falta de papel, toner ou impressora desligada)."
            };

            grpConfirmacao.Controls.Add(lblNotaSpooler);
            grpConfirmacao.Controls.Add(pnlBotoesConfirmacao);
            pnlDiagnosticoEConfirmacao.Controls.Add(grpConfirmacao, 0, 0);

            // Painel Diagnóstico Final
            var grpDiagFinal = new GroupBox
            {
                Text = "Diagnóstico Final (Causa Raiz)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            _txtDiagnosticoFinal = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 40, 80),
                BackColor = Color.FromArgb(248, 250, 252),
                Text = "Aguardando execução do diagnóstico."
            };
            grpDiagFinal.Controls.Add(_txtDiagnosticoFinal);
            pnlDiagnosticoEConfirmacao.Controls.Add(grpDiagFinal, 1, 0);

            // 5. Rodapé (Botões Ação)
            var pnlRodape = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 4, 0, 0)
            };
            _btnAbrirLogs = new Button
            {
                Text = "Abrir pasta de logs",
                Width = 160,
                Height = 32,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _btnAbrirLogs.Click += (s, e) => AbrirPastaLogs();

            _btnCopiarDiag = new Button
            {
                Text = "Copiar diagnóstico completo",
                Width = 200,
                Height = 32,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            _btnCopiarDiag.Click += (s, e) => CopiarDiagnosticoParaClipboard();

            pnlRodape.Controls.Add(_btnAbrirLogs);
            pnlRodape.Controls.Add(_btnCopiarDiag);
            painelPrincipal.Controls.Add(pnlRodape, 0, 4);
        }

        private void ConfigurarGridEtapas()
        {
            _gridEtapas.Columns.Clear();
            _gridEtapas.Columns.Add("Numero", "#");
            _gridEtapas.Columns["Numero"].Width = 35;

            _gridEtapas.Columns.Add("Etapa", "Etapa de Diagnóstico");
            _gridEtapas.Columns["Etapa"].Width = 190;

            _gridEtapas.Columns.Add("Status", "Status");
            _gridEtapas.Columns["Status"].Width = 100;

            _gridEtapas.Columns.Add("Duracao", "Duração");
            _gridEtapas.Columns["Duracao"].Width = 75;

            _gridEtapas.Columns.Add("Mensagem", "Mensagem / Detalhes");
            _gridEtapas.Columns["Mensagem"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;

            InicializarLinhasEtapasPadrao();
        }

        private void InicializarLinhasEtapasPadrao()
        {
            _gridEtapas.Rows.Clear();
            var nomesEtapas = new[]
            {
                "1. AMBIENTE",
                "2. ARQUIVO",
                "3. VISUALIZADOR",
                "4. SNAPSHOT DAS FILAS",
                "5. EXECUÇÃO",
                "6. PDF (INTERMEDIÁRIO)",
                "7. DETECÇÃO DO(S) JOB(S)",
                "8. CONCLUSÃO (SPOOLER)",
                "9. CONFIRMAÇÃO VISUAL"
            };

            for (int i = 0; i < nomesEtapas.Length; i++)
            {
                _gridEtapas.Rows.Add((i + 1).ToString(), nomesEtapas[i], "Pendente", "-", "Aguardando execução.");
                EstilizarLinhaEtapa(i, StatusEtapa.Pendente);
            }
        }

        private void ConfigurarGridImpressoras()
        {
            _gridImpressoras.Columns.Clear();
            _gridImpressoras.Columns.Add("Nome", "Nome da Impressora");
            _gridImpressoras.Columns["Nome"].Width = 240;

            _gridImpressoras.Columns.Add("Driver", "Driver");
            _gridImpressoras.Columns["Driver"].Width = 200;

            _gridImpressoras.Columns.Add("Porta", "Porta");
            _gridImpressoras.Columns["Porta"].Width = 140;

            _gridImpressoras.Columns.Add("Padrao", "Padrão");
            _gridImpressoras.Columns["Padrao"].Width = 60;

            _gridImpressoras.Columns.Add("Rdp", "Redir. RDP");
            _gridImpressoras.Columns["Rdp"].Width = 85;

            _gridImpressoras.Columns.Add("Status", "Status Atual");
            _gridImpressoras.Columns["Status"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }

        #endregion

        #region Inicialização e Configuração

        private void CarregarConfiguracaoOuOferecerPadrao()
        {
            var caminhoPadrao = ConfigLoader.ObterCaminhoPadrao();

            try
            {
                _config = ConfigLoader.Carregar(caminhoPadrao);
                PopularPerfis();
            }
            catch (ConfigException cex)
            {
                RegistrarLogTempoReal($"Falha de configuração ({cex.Campo}): {cex.Message}");

                var resposta = MessageBox.Show(
                    $"Problema na configuração do programa:\n\n{cex.Message}\n\nCampo: '{cex.Campo}'\n\nDeseja criar o arquivo de configuração padrão em '{caminhoPadrao}'?",
                    "Configuração Inexistente ou Inválida",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (resposta == DialogResult.Yes)
                {
                    try
                    {
                        ConfigLoader.CriarConfigPadrao(caminhoPadrao);
                        _config = ConfigLoader.Carregar(caminhoPadrao);
                        PopularPerfis();
                        RegistrarLogTempoReal($"Arquivo de configuração padrão criado com êxito em '{caminhoPadrao}'.");
                    }
                    catch (Exception exCriar)
                    {
                        MessageBox.Show(
                            $"Não foi possível criar o arquivo de configuração padrão:\n\n{exCriar.Message}",
                            "Erro ao Criar Configuração",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                RegistrarLogTempoReal($"Erro inesperado ao carregar configuração: {ex.Message}");
            }
        }

        private void PopularPerfis()
        {
            _cboPerfil.Items.Clear();
            if (_config == null || _config.Perfis == null) return;

            foreach (var p in _config.Perfis)
            {
                _cboPerfil.Items.Add(p);
            }

            if (!string.IsNullOrWhiteSpace(_config.PerfilPadrao))
            {
                var perfilPadrao = _config.Perfis.FirstOrDefault(p =>
                    string.Equals(p.Nome, _config.PerfilPadrao, StringComparison.OrdinalIgnoreCase));
                if (perfilPadrao != null)
                {
                    _cboPerfil.SelectedItem = perfilPadrao;
                }
            }

            if (_cboPerfil.SelectedIndex == -1 && _cboPerfil.Items.Count > 0)
            {
                _cboPerfil.SelectedIndex = 0;
            }

            AtualizarFiltroArquivo();
        }

        private void AtualizarBannerSessao()
        {
            try
            {
                var ambiente = AmbienteInfo.Coletar(_config?.PastaLogs);
                var sessao = ambiente.EhRdp
                    ? $"RDP ({ambiente.SessionName}, Cliente: {(string.IsNullOrEmpty(ambiente.ClientName) ? "N/D" : ambiente.ClientName)})"
                    : $"Local/Console ({ambiente.SessionName})";

                _lblBannerSessao.Text = $" Sessão: {sessao} | Usuário: {ambiente.Dominio}\\{ambiente.Usuario} | Estação: {ambiente.Maquina} | Processo {ambiente.ProcessoBitness}";
            }
            catch (Exception ex)
            {
                _lblBannerSessao.Text = $" Sessão Windows: Falha ao detectar ({ex.Message})";
            }
        }

        private void AtualizarGridImpressoras()
        {
            try
            {
                _gridImpressoras.Rows.Clear();
                var impressoras = _fonteFilas.ObterImpressoras()?.ToList() ?? new List<InfoImpressora>();

                foreach (var imp in impressoras)
                {
                    int rowId = _gridImpressoras.Rows.Add(
                        imp.Nome,
                        imp.Driver,
                        imp.Porta,
                        imp.EhPadrao ? "SIM" : "NÃO",
                        imp.RedirecionadaRdp ? "SIM" : "NÃO",
                        imp.StatusTexto);

                    if (imp.EhPadrao)
                    {
                        _gridImpressoras.Rows[rowId].DefaultCellStyle.Font = new Font(_gridImpressoras.Font, FontStyle.Bold);
                        _gridImpressoras.Rows[rowId].DefaultCellStyle.BackColor = Color.FromArgb(240, 250, 240);
                    }
                    if (imp.IsInError || imp.IsOffline)
                    {
                        _gridImpressoras.Rows[rowId].DefaultCellStyle.ForeColor = Color.DarkRed;
                    }
                }

                RegistrarLogTempoReal($"Lista de impressoras atualizada: {impressoras.Count} fila(s) detectada(s).");
            }
            catch (Exception ex)
            {
                RegistrarLogTempoReal($"Falha ao listar impressoras: {ex.Message}");
            }
        }

        private void AplicarArgumentosLinhaDeComando()
        {
            if (!string.IsNullOrWhiteSpace(_argPerfil) && _config != null)
            {
                var perfilMatch = _config.Perfis.FirstOrDefault(p =>
                    string.Equals(p.Nome, _argPerfil, StringComparison.OrdinalIgnoreCase));
                if (perfilMatch != null)
                {
                    _cboPerfil.SelectedItem = perfilMatch;
                }
            }

            if (!string.IsNullOrWhiteSpace(_argArquivo))
            {
                _txtArquivo.Text = _argArquivo;
            }

            if (_argVias.HasValue && _argVias.Value >= 1 && _argVias.Value <= 10)
            {
                _numVias.Value = _argVias.Value;
            }
        }

        #endregion

        #region Ações de Usuário

        private void SelecionarArquivo()
        {
            try
            {
                var perfil = _cboPerfil.SelectedItem as PerfilImpressao;
                string filtro = ConstruirFiltroDialogo(perfil);

                using (var ofd = new OpenFileDialog())
                {
                    ofd.Title = "Selecionar Documento para Diagnóstico de Impressão";
                    ofd.Filter = filtro;
                    ofd.CheckFileExists = true;

                    if (!string.IsNullOrWhiteSpace(_ultimaPastaSelecionada) && Directory.Exists(_ultimaPastaSelecionada))
                    {
                        ofd.InitialDirectory = _ultimaPastaSelecionada;
                    }
                    else if (!string.IsNullOrWhiteSpace(_txtArquivo.Text))
                    {
                        var dir = Path.GetDirectoryName(_txtArquivo.Text.Trim('\"'));
                        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir))
                        {
                            ofd.InitialDirectory = dir;
                        }
                    }

                    if (ofd.ShowDialog(this) == DialogResult.OK)
                    {
                        _txtArquivo.Text = ofd.FileName;
                        _ultimaPastaSelecionada = Path.GetDirectoryName(ofd.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao abrir diálogo de seleção de arquivo: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string ConstruirFiltroDialogo(PerfilImpressao perfil)
        {
            if (perfil == null || perfil.ExtensoesPermitidas == null || perfil.ExtensoesPermitidas.Count == 0)
            {
                return "Todos os Arquivos (*.*)|*.*";
            }

            var partesExt = string.Join(";", perfil.ExtensoesPermitidas.Select(e => "*" + e));
            var nomesExt = string.Join(", ", perfil.ExtensoesPermitidas.Select(e => e.ToUpperInvariant()));

            return $"Arquivos do Perfil ({nomesExt})|{partesExt}|Todos os Arquivos (*.*)|*.*";
        }

        private void AtualizarFiltroArquivo()
        {
            var perfil = _cboPerfil.SelectedItem as PerfilImpressao;
            if (perfil != null && string.IsNullOrWhiteSpace(_txtArquivo.Text))
            {
                // Sugestão de exemplo se estiver vazio
                if (perfil.ExtensoesPermitidas != null && perfil.ExtensoesPermitidas.Contains(".rtf"))
                {
                    // Sem arquivo padrão forçado
                }
            }
        }

        private async void IniciarTeste(bool apenasAmbiente)
        {
            if (_emExecucao) return;

            var perfil = _cboPerfil.SelectedItem as PerfilImpressao;
            if (perfil == null)
            {
                MessageBox.Show("Nenhum perfil de impressão selecionado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var arquivo = _txtArquivo.Text.Trim();
            if (string.IsNullOrWhiteSpace(arquivo) && !apenasAmbiente)
            {
                MessageBox.Show("Informe ou selecione o arquivo para impressão antes de iniciar.", "Arquivo Não Informado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtArquivo.Focus();
                return;
            }

            int vias = (int)_numVias.Value;

            HabilitarControles(false);
            _emExecucao = true;
            _btnPapelSim.Enabled = false;
            _btnPapelNao.Enabled = false;
            _btnPapelNaoSei.Enabled = false;
            InicializarLinhasEtapasPadrao();
            _txtDiagnosticoFinal.Text = "Executando teste de diagnóstico...";

            RegistrarLogTempoReal("================================================================================");
            RegistrarLogTempoReal($"Iniciando {(apenasAmbiente ? "Diagnóstico do Ambiente" : "Teste Completo de Impressão")} - Perfil: '{perfil.Nome}' | Vias: {vias}");
            RegistrarLogTempoReal($"Arquivo: '{arquivo}'");

            try
            {
                var engine = new FluxoTesteEngine(_config, _fonteFilas);

                ResultadoTeste resultado = null;
                await Task.Run(() =>
                {
                    resultado = engine.Executar(
                        perfil,
                        arquivo,
                        vias,
                        apenasAmbiente,
                        etapa =>
                        {
                            this.Invoke(new Action(() => AtualizarEtapaNaGrid(etapa)));
                        },
                        msg =>
                        {
                            this.Invoke(new Action(() => RegistrarLogTempoReal(msg)));
                        });
                });

                _ultimoResultado = resultado;
                _txtDiagnosticoFinal.Text = resultado.DiagnosticoFinal;

                // Se completou a etapa 8 sem falhas, habilita a confirmação visual
                bool chegouEtapa9 = !apenasAmbiente &&
                                    resultado.Etapas.Any(e => e.Numero == 8 && (e.Status == StatusEtapa.OK || e.Status == StatusEtapa.Aviso));

                if (chegouEtapa9)
                {
                    _btnPapelSim.Enabled = true;
                    _btnPapelNao.Enabled = true;
                    _btnPapelNaoSei.Enabled = true;
                    RegistrarLogTempoReal("Aguardando confirmação do usuário se o documento saiu no papel físico.");
                }
                else
                {
                    // Grava log imediatamente se o teste falhou antes ou foi apenas ambiente
                    LogTeste.Gravar(_ultimoResultado, _config.PastaLogs);
                }
            }
            catch (Exception ex)
            {
                RegistrarLogTempoReal($"Erro inesperado na execução do teste: {ex.Message}");
                MessageBox.Show($"Ocorreu uma exceção não esperada:\n\n{ex.Message}", "Erro de Execução", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _emExecucao = false;
                HabilitarControles(true);
            }
        }

        private void AtualizarEtapaNaGrid(EtapaTeste etapa)
        {
            if (etapa == null) return;
            int rowIndex = etapa.Numero - 1;

            if (rowIndex >= 0 && rowIndex < _gridEtapas.Rows.Count)
            {
                var row = _gridEtapas.Rows[rowIndex];
                row.Cells["Status"].Value = etapa.Status.ToString();
                row.Cells["Duracao"].Value = etapa.Duracao.TotalSeconds > 0
                    ? $"{etapa.Duracao.TotalSeconds:F2}s"
                    : "-";
                row.Cells["Mensagem"].Value = etapa.Mensagem;

                EstilizarLinhaEtapa(rowIndex, etapa.Status);
            }
        }

        private void EstilizarLinhaEtapa(int rowIndex, StatusEtapa status)
        {
            var row = _gridEtapas.Rows[rowIndex];
            switch (status)
            {
                case StatusEtapa.OK:
                    row.Cells["Status"].Style.BackColor = Color.FromArgb(220, 245, 220);
                    row.Cells["Status"].Style.ForeColor = Color.DarkGreen;
                    row.Cells["Status"].Style.Font = new Font(_gridEtapas.Font, FontStyle.Bold);
                    break;

                case StatusEtapa.Aviso:
                    row.Cells["Status"].Style.BackColor = Color.FromArgb(255, 245, 215);
                    row.Cells["Status"].Style.ForeColor = Color.FromArgb(160, 90, 0);
                    row.Cells["Status"].Style.Font = new Font(_gridEtapas.Font, FontStyle.Bold);
                    break;

                case StatusEtapa.Falha:
                    row.Cells["Status"].Style.BackColor = Color.FromArgb(255, 225, 225);
                    row.Cells["Status"].Style.ForeColor = Color.DarkRed;
                    row.Cells["Status"].Style.Font = new Font(_gridEtapas.Font, FontStyle.Bold);
                    break;

                case StatusEtapa.NaoVerificada:
                    row.Cells["Status"].Style.BackColor = Color.FromArgb(240, 240, 240);
                    row.Cells["Status"].Style.ForeColor = Color.Gray;
                    break;

                default:
                    row.Cells["Status"].Style.BackColor = Color.White;
                    row.Cells["Status"].Style.ForeColor = Color.Black;
                    break;
            }
        }

        private void RegistrarRespostaPapel(string resposta)
        {
            if (_ultimoResultado == null) return;

            _ultimoResultado.ConfirmacaoPapel = resposta;
            _btnPapelSim.Enabled = false;
            _btnPapelNao.Enabled = false;
            _btnPapelNaoSei.Enabled = false;

            var etapa9 = _ultimoResultado.ObterOuCriarEtapa(9, "CONFIRMAÇÃO VISUAL");
            if (resposta == "Sim")
            {
                etapa9.Status = StatusEtapa.OK;
                etapa9.Mensagem = "O documento saiu fisicamente no papel (confirmado pelo operador).";
            }
            else if (resposta == "Não")
            {
                etapa9.Status = StatusEtapa.Falha;
                etapa9.Mensagem = "O documento NÃO saiu no papel físico (possível problema físico, cabo, rede ou hardware).";
            }
            else
            {
                etapa9.Status = StatusEtapa.Aviso;
                etapa9.Mensagem = "Confirmação física pendente / operador não soube informar.";
            }

            AtualizarEtapaNaGrid(etapa9);
            _ultimoResultado.AtualizarDiagnosticoFinal();
            _txtDiagnosticoFinal.Text = _ultimoResultado.DiagnosticoFinal;

            RegistrarLogTempoReal($"Confirmação visual informada: '{resposta}'. Diagnóstico final atualizado.");

            // Grava o log atualizado com a confirmação
            LogTeste.Gravar(_ultimoResultado, _config.PastaLogs);
            RegistrarLogTempoReal("Relatório gravado com sucesso no arquivo de log diário.");
        }

        private void HabilitarControles(bool habilitar)
        {
            _btnImprimirCigam.Enabled = habilitar;
            _btnDiagAmbiente.Enabled = habilitar;
            _btnAbrirArquivo.Enabled = habilitar;
            _cboPerfil.Enabled = habilitar;
            _txtArquivo.Enabled = habilitar;
            _numVias.Enabled = habilitar;
        }

        private void CopiarDiagnosticoParaClipboard()
        {
            try
            {
                if (_ultimoResultado == null)
                {
                    MessageBox.Show("Nenhum teste foi executado ainda para copiar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var relatorio = _ultimoResultado.GerarRelatorioCompleto();
                Clipboard.SetText(relatorio);
                MessageBox.Show("Diagnóstico completo copiado para a Área de Transferência com sucesso!\nCole no chamado de suporte do ERP CIGAM.", "Sucesso", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao copiar para a área de transferência: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AbrirPastaLogs()
        {
            try
            {
                var template = _config?.PastaLogs ?? @"%LOCALAPPDATA%\CigamPrintTest\logs";
                var pasta = Environment.ExpandEnvironmentVariables(template);

                if (!Directory.Exists(pasta))
                {
                    Directory.CreateDirectory(pasta);
                }

                Process.Start("explorer.exe", pasta);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Falha ao abrir pasta de logs: {ex.Message}", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RegistrarLogTempoReal(string mensagem)
        {
            if (string.IsNullOrWhiteSpace(mensagem)) return;

            var linha = $"[{DateTime.Now:HH:mm:ss}] {mensagem}";
            if (_txtLogTempoReal.InvokeRequired)
            {
                _txtLogTempoReal.Invoke(new Action(() =>
                {
                    _txtLogTempoReal.AppendText(linha + Environment.NewLine);
                }));
            }
            else
            {
                _txtLogTempoReal.AppendText(linha + Environment.NewLine);
            }
        }

        #endregion
    }
}
