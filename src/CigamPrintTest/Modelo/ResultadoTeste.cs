using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CigamPrintTest.Config;
using CigamPrintTest.Diagnostico;
using CigamPrintTest.Fila;

namespace CigamPrintTest.Modelo
{
    public class ResultadoTeste
    {
        public string CodTeste { get; set; }
        public DateTime DataHora { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public string CigamInstal { get; set; } = string.Empty;
        public PerfilImpressao Perfil { get; set; }
        public string Arquivo { get; set; }
        public int Vias { get; set; }
        public string ComandoFinal { get; set; } = string.Empty;
        public string PastaExecucaoFinal { get; set; } = string.Empty;
        public long TamanhoArquivoBytes { get; set; }
        public AmbienteInfo Ambiente { get; set; }
        public List<EtapaTeste> Etapas { get; set; } = new List<EtapaTeste>();
        public List<InfoJob> JobsDetectados { get; set; } = new List<InfoJob>();
        public Dictionary<string, string> FlagsFilas { get; set; } = new Dictionary<string, string>();
        public string ConfirmacaoPapel { get; set; } = "Não perguntado";
        public string DiagnosticoFinal { get; set; } = string.Empty;

        public static ResultadoTeste CriarNovo(PerfilImpressao perfil, string arquivo, int vias)
        {
            return new ResultadoTeste
            {
                CodTeste = "T" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                DataHora = DateTime.Now,
                Perfil = perfil,
                Arquivo = arquivo ?? string.Empty,
                Vias = Math.Max(1, vias)
            };
        }

        public EtapaTeste ObterOuCriarEtapa(int numero, string nome)
        {
            var etapa = Etapas.FirstOrDefault(e => e.Numero == numero);
            if (etapa == null)
            {
                etapa = new EtapaTeste
                {
                    Numero = numero,
                    Nome = nome,
                    Status = StatusEtapa.Pendente
                };
                Etapas.Add(etapa);
            }
            return etapa;
        }

        public string AtualizarDiagnosticoFinal()
        {
            var primeiraFalha = Etapas.FirstOrDefault(e => e.Status == StatusEtapa.Falha);
            if (primeiraFalha != null)
            {
                switch (primeiraFalha.Numero)
                {
                    case 1:
                        DiagnosticoFinal = "Camada 1 (Ambiente/Permissão): Falha de permissão, diretório temporário ou ambiente da sessão Windows.";
                        break;
                    case 2:
                        DiagnosticoFinal = "Camada 2 (Arquivo): Arquivo não encontrado, corrompido, bloqueado por outro aplicativo ou extensão incompatível.";
                        break;
                    case 3:
                        DiagnosticoFinal = "Camada 3 (Visualizador): CGEditor ausente na estação, caminho incorreto ou perfil de impressão inválido.";
                        break;
                    case 5:
                        DiagnosticoFinal = "Camada 5 (Execução): O CGEditor não abriu ou fechou com código de erro (falha no Process.Start ou exit code).";
                        break;
                    case 6:
                        DiagnosticoFinal = "Camada 6 (Conversor/PDF): O visualizador/conversor não gerou o PDF intermediário (problema fora da impressora).";
                        break;
                    case 7:
                        DiagnosticoFinal = "Camada 7 (Seleção de Impressora/Driver): O CGEditor abriu mas não enviou jobs à fila (impressora não selecionada, diálogo cancelado ou driver recusou).";
                        break;
                    case 8:
                        DiagnosticoFinal = "Camada 8 (Fila/Driver/Porta): Falha na fila de impressão, driver com erro, impressora offline ou porta inacessível.";
                        break;
                    default:
                        DiagnosticoFinal = $"Falha na Etapa {primeiraFalha.Numero} ({primeiraFalha.Nome}): {primeiraFalha.Mensagem}";
                        break;
                }
                return DiagnosticoFinal;
            }

            if (string.Equals(ConfirmacaoPapel, "Não", StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticoFinal = "Problema físico ou porta sem retorno de status: documento foi entregue ao Spooler, mas não saiu na impressora (verifique papel, toner, cabo/rede ou fila travada no hardware).";
                return DiagnosticoFinal;
            }

            if (string.Equals(ConfirmacaoPapel, "Sim", StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticoFinal = "Impressão concluída com sucesso e saída física no papel confirmada pelo usuário.";
                return DiagnosticoFinal;
            }

            if (string.Equals(ConfirmacaoPapel, "Não sei", StringComparison.OrdinalIgnoreCase))
            {
                DiagnosticoFinal = "Documento entregue à fila de impressão (Spooler); aguardando confirmação física na impressora.";
                return DiagnosticoFinal;
            }

            DiagnosticoFinal = "Documento entregue ao subsistema de impressão do Windows com sucesso.";
            return DiagnosticoFinal;
        }

        public string GerarRelatorioCompleto()
        {
            var sb = new StringBuilder();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"DIAGNÓSTICO DE IMPRESSÃO CIGAM - {CodTeste}");
            sb.AppendLine($"Data/Hora:       {DataHora:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Cliente:         {(string.IsNullOrWhiteSpace(Cliente) ? "(não informado)" : Cliente)}");
            sb.AppendLine($"%CIGAM_INSTAL%:  {(string.IsNullOrWhiteSpace(CigamInstal) ? "(não configurado)" : CigamInstal)}");
            sb.AppendLine("================================================================================");

            if (Ambiente != null)
            {
                sb.AppendLine("1. AMBIENTE DA SESSÃO:");
                sb.AppendLine($"   Usuário:     {Ambiente.Dominio}\\{Ambiente.Usuario}");
                sb.AppendLine($"   Máquina:     {Ambiente.Maquina}");
                sb.AppendLine($"   Sessão:      {Ambiente.SessionName} {(Ambiente.EhRdp ? "[Sessão Terminal Services/RDP]" : "[Sessão Local/Console]")}");
                sb.AppendLine($"   CLIENTNAME:  {(string.IsNullOrEmpty(Ambiente.ClientName) ? "(vazio / sessão local)" : Ambiente.ClientName)}");
                sb.AppendLine($"   Arquitetura: Processo {Ambiente.ProcessoBitness} | SO {Ambiente.SistemaOperacionalBitness} ({Ambiente.VersaoSO})");
                sb.AppendLine($"   TEMP:        {Ambiente.TempPath} [{(Ambiente.TempGravavel ? "OK" : "ERRO: " + Ambiente.MensagemErroTemp)}]");
                sb.AppendLine($"   Pasta Logs:  {Ambiente.PastaLogsResolvida} [{(Ambiente.PastaLogsGravavel ? "OK" : "ERRO: " + Ambiente.MensagemErroLogs)}]");
                sb.AppendLine();
            }

            sb.AppendLine("2. PARÂMETROS DO TESTE:");
            sb.AppendLine($"   Perfil:          {Perfil?.Nome ?? "(nenhum)"}");
            sb.AppendLine($"   Arquivo:         {Arquivo} [Tamanho: {TamanhoArquivoBytes:N0} bytes]");
            sb.AppendLine($"   Vias:            {Vias}");
            sb.AppendLine($"   Comando Final:   {ComandoFinal}");
            sb.AppendLine($"   Pasta Execução:  {PastaExecucaoFinal}");
            sb.AppendLine();

            sb.AppendLine("3. ETAPAS DE DIAGNÓSTICO:");
            foreach (var etapa in Etapas.OrderBy(e => e.Numero))
            {
                sb.AppendLine($"   {etapa.ObterLinhaFormatada()}");
                if (!string.IsNullOrWhiteSpace(etapa.Detalhes))
                {
                    sb.AppendLine($"      Detalhes: {etapa.Detalhes}");
                }
            }
            sb.AppendLine();

            if (JobsDetectados.Count > 0)
            {
                sb.AppendLine("4. JOBS DETECTADOS NA FILA DO WINDOWS:");
                foreach (var job in JobsDetectados)
                {
                    sb.AppendLine($"   - Job ID {job.JobId} | Fila: '{job.FilaNome}' | Doc: '{job.NomeDocumento}' | Submitter: '{job.Submitter}' | Submetido: {job.TimeJobSubmitted:HH:mm:ss} | Status: [{job.ObterFlagsAtivas()}]");
                }
                sb.AppendLine();
            }

            if (FlagsFilas.Count > 0)
            {
                sb.AppendLine("5. ESTADO DAS FILAS ENVOLVIDAS:");
                foreach (var kvp in FlagsFilas)
                {
                    sb.AppendLine($"   - Fila '{kvp.Key}': {kvp.Value}");
                }
                sb.AppendLine();
            }

            sb.AppendLine("6. CONFIRMAÇÃO VISUAL:");
            sb.AppendLine($"   O documento saiu no papel? {ConfirmacaoPapel}");
            sb.AppendLine();

            AtualizarDiagnosticoFinal();
            sb.AppendLine("================================================================================");
            sb.AppendLine($"DIAGNÓSTICO FINAL (CAUSA RAIZ):");
            sb.AppendLine($"{DiagnosticoFinal}");
            sb.AppendLine("================================================================================");

            return sb.ToString();
        }
    }
}
