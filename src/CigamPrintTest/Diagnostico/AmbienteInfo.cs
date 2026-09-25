using System;
using System.IO;
using System.Windows.Forms;

namespace CigamPrintTest.Diagnostico
{
    public class AmbienteInfo
    {
        public string Usuario { get; set; } = string.Empty;
        public string Dominio { get; set; } = string.Empty;
        public string Maquina { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public bool EhRdp { get; set; }
        public string ProcessoBitness { get; set; } = string.Empty;
        public string SistemaOperacionalBitness { get; set; } = string.Empty;
        public string VersaoSO { get; set; } = string.Empty;
        public string TempPath { get; set; } = string.Empty;
        public bool TempGravavel { get; set; }
        public string MensagemErroTemp { get; set; } = string.Empty;
        public string PastaLogsResolvida { get; set; } = string.Empty;
        public bool PastaLogsGravavel { get; set; }
        public string MensagemErroLogs { get; set; } = string.Empty;

        public static AmbienteInfo Coletar(string templatePastaLogs)
        {
            var info = new AmbienteInfo
            {
                Usuario = Environment.UserName,
                Dominio = Environment.UserDomainName,
                Maquina = Environment.MachineName,
                ProcessoBitness = Environment.Is64BitProcess ? "64-bit" : "32-bit",
                SistemaOperacionalBitness = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit",
                VersaoSO = Environment.OSVersion.VersionString
            };

            // Informações de Sessão Windows / Terminal Services / RDP
            var sessName = Environment.GetEnvironmentVariable("SESSIONNAME");
            info.SessionName = string.IsNullOrWhiteSpace(sessName) ? "Console" : sessName;

            var clientName = Environment.GetEnvironmentVariable("CLIENTNAME");
            info.ClientName = clientName ?? string.Empty;

            // Detecção de RDP
            bool rdpPorVariavel = info.SessionName.IndexOf("RDP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                  !string.IsNullOrWhiteSpace(info.ClientName);
            bool rdpPorSystemInfo = SystemInformation.TerminalServerSession;
            info.EhRdp = rdpPorVariavel || rdpPorSystemInfo;

            // Verificação de diretório temporário (TEMP)
            try
            {
                info.TempPath = Path.GetTempPath();
                if (!Directory.Exists(info.TempPath))
                {
                    Directory.CreateDirectory(info.TempPath);
                }

                var arquivoTesteTemp = Path.Combine(info.TempPath, $"cpt_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(arquivoTesteTemp, "test");
                File.Delete(arquivoTesteTemp);
                info.TempGravavel = true;
            }
            catch (Exception ex)
            {
                info.TempGravavel = false;
                info.MensagemErroTemp = ex.Message;
            }

            // Verificação da pasta de logs
            try
            {
                var template = string.IsNullOrWhiteSpace(templatePastaLogs)
                    ? @"%LOCALAPPDATA%\CigamPrintTest\logs"
                    : templatePastaLogs;

                info.PastaLogsResolvida = Environment.ExpandEnvironmentVariables(template);
                if (!Directory.Exists(info.PastaLogsResolvida))
                {
                    Directory.CreateDirectory(info.PastaLogsResolvida);
                }

                var arquivoTesteLog = Path.Combine(info.PastaLogsResolvida, $"cpt_test_{Guid.NewGuid():N}.tmp");
                File.WriteAllText(arquivoTesteLog, "test");
                File.Delete(arquivoTesteLog);
                info.PastaLogsGravavel = true;
            }
            catch (Exception ex)
            {
                info.PastaLogsGravavel = false;
                info.MensagemErroLogs = ex.Message;
            }

            return info;
        }

        public string ObterResumoFormatado()
        {
            var tipoSessao = EhRdp ? $"RDP ({SessionName}, Cliente: {(string.IsNullOrEmpty(ClientName) ? "N/D" : ClientName)})" : $"Local ({SessionName})";
            return $"Usuário: {Dominio}\\{Usuario} | Estação: {Maquina} | Sessão: {tipoSessao} | Processo: {ProcessoBitness} (SO: {SistemaOperacionalBitness}) | TEMP: {(TempGravavel ? "OK" : "ERRO: " + MensagemErroTemp)} | Logs: {(PastaLogsGravavel ? "OK (" + PastaLogsResolvida + ")" : "ERRO: " + MensagemErroLogs)}";
        }
    }
}
