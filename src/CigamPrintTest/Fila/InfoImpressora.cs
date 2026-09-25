namespace CigamPrintTest.Fila
{
    public class InfoImpressora
    {
        public string Nome { get; set; } = string.Empty;
        public string Driver { get; set; } = string.Empty;
        public string Porta { get; set; } = string.Empty;
        public bool EhPadrao { get; set; }
        public bool RedirecionadaRdp { get; set; }
        public string StatusTexto { get; set; } = string.Empty;

        // Flags da fila de impressão
        public bool IsOffline { get; set; }
        public bool IsInError { get; set; }
        public bool IsOutOfPaper { get; set; }
        public bool IsPaperJammed { get; set; }
        public bool IsPaused { get; set; }
        public bool IsDoorOpened { get; set; }
        public bool NeedUserIntervention { get; set; }
        public bool IsNotAvailable { get; set; }
        public int NumberOfJobs { get; set; }

        public string ObterDescricaoFlags()
        {
            var flags = new System.Collections.Generic.List<string>();
            if (IsOffline) flags.Add("Offline");
            if (IsInError) flags.Add("Em Erro");
            if (IsOutOfPaper) flags.Add("Sem Papel");
            if (IsPaperJammed) flags.Add("Papel Atolado");
            if (IsPaused) flags.Add("Pausada");
            if (IsDoorOpened) flags.Add("Porta Aberta");
            if (NeedUserIntervention) flags.Add("Intervenção Manual");
            if (IsNotAvailable) flags.Add("Indisponível");
            if (flags.Count == 0) flags.Add("Pronta");

            return $"{string.Join(", ", flags)} (Jobs na fila: {NumberOfJobs})";
        }
    }
}
