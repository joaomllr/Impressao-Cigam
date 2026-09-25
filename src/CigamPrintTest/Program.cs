using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using CigamPrintTest.Config;

namespace CigamPrintTest
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Handler global para exceções de threads WinForms
            Application.ThreadException += (sender, e) =>
            {
                TratarExcecaoGlobal(e.Exception, "Application.ThreadException");
            };

            // Handler global para exceções não tratadas no domínio da aplicação
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                TratarExcecaoGlobal(ex, "AppDomain.UnhandledException");
            };

            string arquivo = null;
            string perfil = null;
            int? vias = null;
            bool auto = false;

            try
            {
                var argsNormalizados = NormalizarArgumentos(args);

                // Parser de argumentos de linha de comando:
                // CigamPrintTest.exe [--arquivo "caminho"] [--perfil "nome"] [--vias N] [--auto]
                for (int i = 0; i < argsNormalizados.Length; i++)
                {
                    var arg = argsNormalizados[i].Trim();

                    if (string.Equals(arg, "--auto", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "-auto", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(arg, "/auto", StringComparison.OrdinalIgnoreCase))
                    {
                        auto = true;
                    }
                    else if (string.Equals(arg, "--arquivo", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "-a", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "/arquivo", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < argsNormalizados.Length)
                        {
                            arquivo = argsNormalizados[++i].Trim('\"');
                        }
                    }
                    else if (string.Equals(arg, "--perfil", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "-p", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "/perfil", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < argsNormalizados.Length)
                        {
                            perfil = argsNormalizados[++i].Trim('\"');
                        }
                    }
                    else if (string.Equals(arg, "--vias", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "-v", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "/vias", StringComparison.OrdinalIgnoreCase))
                    {
                        if (i + 1 < argsNormalizados.Length && int.TryParse(argsNormalizados[++i], out var v))
                        {
                            vias = Math.Max(1, Math.Min(10, v));
                        }
                    }
                    else if (!arg.StartsWith("-") && !arg.StartsWith("/") && string.IsNullOrEmpty(arquivo))
                    {
                        // Suporte a argumento posicional direto: CigamPrintTest.exe "C:\caminho\danfe.rtf"
                        arquivo = arg.Trim('\"');
                    }
                }
            }
            catch (Exception ex)
            {
                TratarExcecaoGlobal(ex, "Parsing de argumentos");
            }

            try
            {
                // Verifica se deve abrir o assistente de configuração inicial:
                // - config ausente, ou "configurado" = false/ausente, ou executável do perfil padrão inexistente
                if (ConfigLoader.DeveExibirAssistente())
                {
                    ConfigApp configParcial = null;
                    try
                    {
                        if (File.Exists(ConfigLoader.ObterCaminhoPadrao()))
                        {
                            configParcial = ConfigLoader.Carregar();
                        }
                    }
                    catch
                    {
                        // Se estiver corrompido, abre assistente limpo
                    }

                    using (var assistente = new FormConfiguracaoInicial(configParcial))
                    {
                        var dr = assistente.ShowDialog();
                        if (dr != DialogResult.OK)
                        {
                            // Se o usuário cancelou e o sistema ainda não está configurado, encerra
                            if (ConfigLoader.DeveExibirAssistente())
                            {
                                return;
                            }
                        }
                    }
                }

                var mainForm = new MainForm(arquivo, perfil, vias, auto);
                Application.Run(mainForm);
            }
            catch (Exception ex)
            {
                TratarExcecaoGlobal(ex, "Application.Run");
            }
        }

        private static void TratarExcecaoGlobal(Exception ex, string origem)
        {
            var msg = ex != null ? ex.ToString() : "Exceção nula ou não identificada.";

            try
            {
                // Gravar log de emergência na pasta temporária ou de logs
                var pastaLogs = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\CigamPrintTest\logs");
                if (!Directory.Exists(pastaLogs))
                {
                    Directory.CreateDirectory(pastaLogs);
                }

                var arquivoLogCrash = Path.Combine(pastaLogs, "CigamPrintTest_Crash.log");
                var cabecalho = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Origem: {origem}{Environment.NewLine}{msg}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
                File.AppendAllText(arquivoLogCrash, cabecalho, Encoding.UTF8);
            }
            catch
            {
                // Ignora falha de escrita no log de emergência
            }

            try
            {
                MessageBox.Show(
                    $"Ocorreu um erro não esperado ({origem}):\n\n{ex?.Message}\n\nO erro foi capturado e gravado no log de diagnóstico.",
                    "Diagnóstico CIGAM - Erro Não Esperado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch
            {
                // Se a interface falhar completamente, não encerra abruptamente sem tentar
            }
        }

        private static string[] NormalizarArgumentos(string[] args)
        {
            if (args == null || args.Length == 0) return new string[0];

            if (args.Length == 1 && (args[0].Contains("--") || args[0].Contains("-a") || args[0].Contains("/a") || args[0].Contains("-p") || args[0].Contains("/p")))
            {
                var lista = new System.Collections.Generic.List<string>();
                var sb = new StringBuilder();
                bool emAspas = false;

                foreach (char c in args[0])
                {
                    if (c == '\"')
                    {
                        emAspas = !emAspas;
                    }
                    else if (c == ' ' && !emAspas)
                    {
                        if (sb.Length > 0)
                        {
                            lista.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                if (sb.Length > 0)
                {
                    lista.Add(sb.ToString());
                }

                return lista.ToArray();
            }

            return args;
        }
    }
}
