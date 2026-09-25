using System;
using System.Diagnostics;
using System.IO;
using CigamPrintTest.Config;
using CigamPrintTest.Erros;

namespace CigamPrintTest.Execucao
{
    public class ResultadoExecucaoProcesso
    {
        public bool Sucesso { get; set; }
        public int Pid { get; set; }
        public int? ExitCode { get; set; }
        public string ComandoCompleto { get; set; }
        public string ExecutavelResolvido { get; set; }
        public string ArgumentosResolvidos { get; set; }
        public string PastaExecucaoResolvida { get; set; }
        public string Mensagem { get; set; }
        public Exception Excecao { get; set; }
    }

    public static class ExecutorComando
    {
        /// <summary>
        /// Substitui os placeholders e expande variáveis de ambiente no modelo de texto.
        /// Placeholders suportados: {arquivo}, {arquivoSemAspas}, {nomeArquivo}, {nomeSemExtensao},
        /// {pasta}, {codTeste}, {usuario}, {maquina}, {via}.
        /// </summary>
        public static string SubstituirPlaceholders(string template, string caminhoArquivo, string codTeste = "", int via = 1)
        {
            if (string.IsNullOrEmpty(template))
            {
                return string.Empty;
            }

            var arquivoSemAspas = (caminhoArquivo ?? string.Empty).Trim('\"');
            var arquivoComAspas = $"\"{arquivoSemAspas}\"";

            string nomeArquivo = string.Empty;
            string nomeSemExtensao = string.Empty;
            string pasta = string.Empty;

            if (!string.IsNullOrEmpty(arquivoSemAspas))
            {
                try
                {
                    nomeArquivo = Path.GetFileName(arquivoSemAspas);
                    nomeSemExtensao = Path.GetFileNameWithoutExtension(arquivoSemAspas);
                    pasta = Path.GetDirectoryName(arquivoSemAspas) ?? string.Empty;
                }
                catch
                {
                    // Em caso de caracteres especiais no caminho não resolvido pelo Path
                    nomeArquivo = arquivoSemAspas;
                    nomeSemExtensao = arquivoSemAspas;
                }
            }

            var usuario = Environment.UserName;
            var maquina = Environment.MachineName;

            var resultado = template;

            // Substituir placeholders específicos
            resultado = resultado.Replace("{arquivoSemAspas}", arquivoSemAspas);
            resultado = resultado.Replace("{arquivo}", arquivoComAspas);
            resultado = resultado.Replace("{nomeArquivo}", nomeArquivo);
            resultado = resultado.Replace("{nomeSemExtensao}", nomeSemExtensao);
            resultado = resultado.Replace("{pasta}", pasta);
            resultado = resultado.Replace("{codTeste}", codTeste ?? string.Empty);
            resultado = resultado.Replace("{usuario}", usuario);
            resultado = resultado.Replace("{maquina}", maquina);
            resultado = resultado.Replace("{via}", via.ToString());

            // Expandir variáveis de ambiente do Windows (%VAR%)
            resultado = Environment.ExpandEnvironmentVariables(resultado);

            return resultado;
        }

        /// <summary>
        /// Resolve a pasta de execução configurada para o perfil. Se vazia, utiliza a pasta do executável.
        /// </summary>
        public static string ResolverPastaExecucao(PerfilImpressao perfil, string caminhoArquivo, string codTeste = "")
        {
            if (perfil == null)
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }

            if (!string.IsNullOrWhiteSpace(perfil.PastaExecucao))
            {
                var resolvida = SubstituirPlaceholders(perfil.PastaExecucao, caminhoArquivo, codTeste);
                if (!string.IsNullOrWhiteSpace(resolvida))
                {
                    return resolvida;
                }
            }

            var exeResolvido = ResolverExecutavel(perfil, caminhoArquivo, codTeste);
            if (!string.IsNullOrWhiteSpace(exeResolvido))
            {
                try
                {
                    var dir = Path.GetDirectoryName(exeResolvido);
                    if (!string.IsNullOrWhiteSpace(dir))
                    {
                        return dir;
                    }
                }
                catch
                {
                    // Ignora erro de parsing e cai no default
                }
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// Resolve o caminho completo do executável, expandindo variáveis de ambiente.
        /// </summary>
        public static string ResolverExecutavel(PerfilImpressao perfil, string caminhoArquivo = "", string codTeste = "")
        {
            if (perfil == null || string.IsNullOrWhiteSpace(perfil.Executavel))
            {
                return string.Empty;
            }

            return SubstituirPlaceholders(perfil.Executavel, caminhoArquivo, codTeste);
        }

        /// <summary>
        /// Monta a linha de comando completa para exibição e diagnóstico.
        /// Exemplo: "C:\Cigam\Laminort\CIGAM11\CGEditor.exe" -VG: -A:C:\x\DANFE30000044312.rtf
        /// </summary>
        public static string MontarComandoCompleto(PerfilImpressao perfil, string caminhoArquivo, string codTeste = "", int via = 1)
        {
            var exe = ResolverExecutavel(perfil, caminhoArquivo, codTeste);
            var args = SubstituirPlaceholders(perfil?.Argumentos ?? string.Empty, caminhoArquivo, codTeste, via);

            if (string.IsNullOrWhiteSpace(args))
            {
                return $"\"{exe}\"";
            }

            return $"\"{exe}\" {args}";
        }

        /// <summary>
        /// Inicia o processo conforme configurado no perfil.
        /// </summary>
        public static ResultadoExecucaoProcesso Executar(
            PerfilImpressao perfil,
            string caminhoArquivo,
            string codTeste = "",
            int via = 1,
            Action<string> logCallback = null)
        {
            var resultado = new ResultadoExecucaoProcesso();

            var executavel = ResolverExecutavel(perfil, caminhoArquivo, codTeste);
            var argumentos = SubstituirPlaceholders(perfil?.Argumentos ?? string.Empty, caminhoArquivo, codTeste, via);
            var pastaExecucao = ResolverPastaExecucao(perfil, caminhoArquivo, codTeste);
            var comandoCompleto = MontarComandoCompleto(perfil, caminhoArquivo, codTeste, via);

            resultado.ExecutavelResolvido = executavel;
            resultado.ArgumentosResolvidos = argumentos;
            resultado.PastaExecucaoResolvida = pastaExecucao;
            resultado.ComandoCompleto = comandoCompleto;

            if (!File.Exists(executavel))
            {
                resultado.Sucesso = false;
                resultado.Mensagem = $"Executável não encontrado: '{executavel}'";
                return resultado;
            }

            var psi = new ProcessStartInfo
            {
                FileName = executavel,
                Arguments = argumentos,
                WorkingDirectory = Directory.Exists(pastaExecucao) ? pastaExecucao : AppDomain.CurrentDomain.BaseDirectory,
                UseShellExecute = false,
                CreateNoWindow = false
            };

            logCallback?.Invoke($"Iniciando processo (Via {via}): {comandoCompleto} [Pasta: {psi.WorkingDirectory}]");

            Process processo = null;
            try
            {
                processo = Process.Start(psi);
                if (processo == null)
                {
                    resultado.Sucesso = false;
                    resultado.Mensagem = "Falha ao iniciar processo: Process.Start retornou nulo.";
                    return resultado;
                }

                resultado.Pid = processo.Id;
                logCallback?.Invoke($"Processo iniciado com PID {processo.Id}.");

                if (perfil != null && perfil.EsperarProcesso)
                {
                    int timeout = perfil.TimeoutProcessoMs > 0 ? perfil.TimeoutProcessoMs : 60000;
                    logCallback?.Invoke($"Aguardando término do processo (timeout: {timeout} ms)...");

                    bool finalizou = processo.WaitForExit(timeout);
                    if (!finalizou)
                    {
                        try
                        {
                            processo.Kill();
                        }
                        catch { }

                        resultado.Sucesso = false;
                        resultado.Mensagem = $"O processo (PID {processo.Id}) excedeu o tempo limite de espera ({timeout / 1000}s) e foi finalizado.";
                        return resultado;
                    }

                    resultado.ExitCode = processo.ExitCode;
                    if (processo.ExitCode != 0)
                    {
                        resultado.Sucesso = false;
                        resultado.Mensagem = $"O processo finalizou com código de saída de erro: {processo.ExitCode}.";
                        return resultado;
                    }

                    resultado.Sucesso = true;
                    resultado.Mensagem = $"Processo concluído com êxito (Exit Code: 0).";
                    return resultado;
                }
                else
                {
                    // Comportamento do CIGAM: Invoke OS Cmd sem esperar término
                    resultado.Sucesso = true;
                    resultado.Mensagem = $"Processo disparado em segundo plano (PID: {processo.Id}) sem esperar término (comportamento CIGAM).";
                    return resultado;
                }
            }
            catch (Exception ex)
            {
                var erroDiag = CatalogoErros.ObterErro(ex);
                resultado.Sucesso = false;
                resultado.Excecao = ex;
                resultado.Mensagem = $"Falha ao executar o processo: {erroDiag}";
                return resultado;
            }
            finally
            {
                if (processo != null && (perfil == null || perfil.EsperarProcesso))
                {
                    processo.Dispose();
                }
            }
        }
    }
}
