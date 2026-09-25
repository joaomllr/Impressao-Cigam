using System.Collections.Generic;

namespace CigamPrintTest.Config
{
    /// <summary>
    /// Configuração global da aplicação carregada de CigamPrintTest.config.json.
    /// </summary>
    public class ConfigApp
    {
        public string PastaLogs { get; set; } = @"%LOCALAPPDATA%\CigamPrintTest\logs";
        public int TimeoutDeteccaoJobMs { get; set; } = 60000;
        public int TimeoutConclusaoJobMs { get; set; } = 120000;
        public int IntervaloPollingMs { get; set; } = 500;
        public string PerfilPadrao { get; set; } = "CIGAM - DANFE (CGEditor)";
        public List<PerfilImpressao> Perfis { get; set; } = new List<PerfilImpressao>();
    }
}
