using System;

namespace CigamPrintTest.Modelo
{
    public enum StatusEtapa
    {
        Pendente,
        OK,
        Aviso,
        Falha,
        NaoVerificada
    }

    public class EtapaTeste
    {
        public int Numero { get; set; }
        public string Nome { get; set; } = string.Empty;
        public StatusEtapa Status { get; set; } = StatusEtapa.Pendente;
        public TimeSpan Duracao { get; set; } = TimeSpan.Zero;
        public string Mensagem { get; set; } = string.Empty;
        public string Detalhes { get; set; } = string.Empty;

        public string ObterIconeTexto()
        {
            switch (Status)
            {
                case StatusEtapa.OK: return "[OK]";
                case StatusEtapa.Aviso: return "[AVISO]";
                case StatusEtapa.Falha: return "[FALHA]";
                case StatusEtapa.NaoVerificada: return "[IGNORADA]";
                default: return "[PEND]";
            }
        }

        public string ObterLinhaFormatada()
        {
            var tempo = Duracao.TotalSeconds < 60
                ? $"{Duracao.TotalSeconds:F2}s"
                : $"{Duracao.Minutes:D2}:{Duracao.Seconds:D2}.{Duracao.Milliseconds / 100:D1}";

            return $"{ObterIconeTexto(),-10} {tempo,6} | {Numero}. {Nome}: {Mensagem}";
        }
    }
}
