using System;
using System.IO;
using System.Text;
using CigamPrintTest.Modelo;

namespace CigamPrintTest.Log
{
    public static class LogTeste
    {
        private static readonly object _syncLock = new object();

        public static string ObterCaminhoArquivoLog(string pastaLogs, DateTime data)
        {
            var pastaResolvida = Environment.ExpandEnvironmentVariables(
                string.IsNullOrWhiteSpace(pastaLogs) ? @"%LOCALAPPDATA%\CigamPrintTest\logs" : pastaLogs);

            var nomeArquivo = $"CigamPrintTest_{data:yyyy-MM-dd}.log";
            return Path.Combine(pastaResolvida, nomeArquivo);
        }

        public static bool Gravar(ResultadoTeste resultado, string pastaLogs)
        {
            if (resultado == null)
            {
                return false;
            }

            try
            {
                var caminhoArquivo = ObterCaminhoArquivoLog(pastaLogs, resultado.DataHora);
                var pasta = Path.GetDirectoryName(caminhoArquivo);
                if (!string.IsNullOrEmpty(pasta) && !Directory.Exists(pasta))
                {
                    Directory.CreateDirectory(pasta);
                }

                var relatorio = resultado.GerarRelatorioCompleto();

                lock (_syncLock)
                {
                    using (var sw = new StreamWriter(caminhoArquivo, true, Encoding.UTF8))
                    {
                        sw.WriteLine(relatorio);
                        sw.WriteLine(); // Linha em branco separando os blocos de teste
                    }
                }

                return true;
            }
            catch
            {
                // Conforme restrição 5: nenhuma exceção pode fechar o programa
                return false;
            }
        }
    }
}
