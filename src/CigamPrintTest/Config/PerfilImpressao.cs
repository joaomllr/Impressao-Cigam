using System;
using System.Collections.Generic;

namespace CigamPrintTest.Config
{
    /// <summary>
    /// Representa o perfil de execução e impressão para o visualizador (ex: CGEditor ou Notepad).
    /// </summary>
    public class PerfilImpressao
    {
        public string Nome { get; set; } = string.Empty;
        public string ComandoCigam { get; set; } = string.Empty;
        public string CigamInstal { get; set; } = string.Empty;
        public string Executavel { get; set; } = string.Empty;
        public string Argumentos { get; set; } = string.Empty;
        public string PastaExecucao { get; set; } = string.Empty;
        public bool EsperarProcesso { get; set; }
        public int TimeoutProcessoMs { get; set; } = 60000;
        public List<string> ExtensoesPermitidas { get; set; } = new List<string>();
        public string PdfEsperado { get; set; } = string.Empty;

        public override string ToString()
        {
            return Nome;
        }

        public PerfilImpressao Clonar()
        {
            return new PerfilImpressao
            {
                Nome = Nome,
                ComandoCigam = ComandoCigam,
                CigamInstal = CigamInstal,
                Executavel = Executavel,
                Argumentos = Argumentos,
                PastaExecucao = PastaExecucao,
                EsperarProcesso = EsperarProcesso,
                TimeoutProcessoMs = TimeoutProcessoMs,
                ExtensoesPermitidas = new List<string>(ExtensoesPermitidas ?? new List<string>()),
                PdfEsperado = PdfEsperado
            };
        }
    }
}
