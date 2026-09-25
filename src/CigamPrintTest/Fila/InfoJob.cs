using System;
using System.Collections.Generic;

namespace CigamPrintTest.Fila
{
    public class InfoJob
    {
        public int JobId { get; set; }
        public string FilaNome { get; set; } = string.Empty;
        public string NomeDocumento { get; set; } = string.Empty;
        public string Submitter { get; set; } = string.Empty;
        public DateTime TimeJobSubmitted { get; set; }
        public string StatusTexto { get; set; } = string.Empty;

        // Flags de status do Job no Spooler
        public bool IsCompleted { get; set; }
        public bool IsPrinted { get; set; }
        public bool IsInError { get; set; }
        public bool IsOffline { get; set; }
        public bool IsPaperOut { get; set; }
        public bool IsUserIntervention { get; set; }
        public bool IsBlocked { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsPaused { get; set; }
        public bool IsPrinting { get; set; }
        public bool IsSpooling { get; set; }

        public bool TemErro => IsInError || IsOffline || IsPaperOut || IsUserIntervention || IsBlocked || IsDeleted;
        public bool ConcluidoComSucesso => IsCompleted || IsPrinted;

        public string ObterFlagsAtivas()
        {
            var flags = new List<string>();
            if (IsCompleted) flags.Add("Completed");
            if (IsPrinted) flags.Add("Printed");
            if (IsInError) flags.Add("Error");
            if (IsOffline) flags.Add("Offline");
            if (IsPaperOut) flags.Add("PaperOut");
            if (IsUserIntervention) flags.Add("UserIntervention");
            if (IsBlocked) flags.Add("Blocked");
            if (IsDeleted) flags.Add("Deleted");
            if (IsPaused) flags.Add("Paused");
            if (IsPrinting) flags.Add("Printing");
            if (IsSpooling) flags.Add("Spooling");

            if (flags.Count == 0 && !string.IsNullOrWhiteSpace(StatusTexto))
            {
                flags.Add(StatusTexto);
            }

            return flags.Count > 0 ? string.Join(", ", flags) : "Nenhum flag ativo";
        }
    }
}
