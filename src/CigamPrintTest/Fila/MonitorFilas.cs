using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CigamPrintTest.Fila
{
    public class SnapshotFilas
    {
        public DateTime T0 { get; set; }
        public HashSet<string> JobsPreexistentes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public int TotalFilasMonitoradas { get; set; }

        public static string CriarChave(string filaNome, int jobId)
        {
            return $"{(filaNome ?? string.Empty).Trim()}:::{jobId}";
        }
    }

    public class ResultadoDeteccaoJobs
    {
        public bool Sucesso { get; set; }
        public bool TemAviso { get; set; }
        public List<InfoJob> JobsDetectados { get; } = new List<InfoJob>();
        public string Mensagem { get; set; } = string.Empty;
    }

    public class ResultadoAcompanhamentoJobs
    {
        public bool Sucesso { get; set; }
        public bool TemAviso { get; set; }
        public string Mensagem { get; set; } = string.Empty;
        public Dictionary<string, string> FlagsFilas { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    public class MonitorFilas
    {
        private readonly IFonteFilas _fonte;

        public MonitorFilas(IFonteFilas fonte = null)
        {
            _fonte = fonte ?? new FonteFilasSpooler();
        }

        public IFonteFilas Fonte => _fonte;

        /// <summary>
        /// Realiza o snapshot de todas as filas e jobs existentes antes da execução do comando.
        /// </summary>
        public SnapshotFilas CriarSnapshot()
        {
            var snapshot = new SnapshotFilas
            {
                T0 = DateTime.Now
            };

            var impressoras = _fonte.ObterImpressoras()?.ToList() ?? new List<InfoImpressora>();
            snapshot.TotalFilasMonitoradas = impressoras.Count;

            var jobs = _fonte.ObterTodosJobs()?.ToList() ?? new List<InfoJob>();
            foreach (var job in jobs)
            {
                snapshot.JobsPreexistentes.Add(SnapshotFilas.CriarChave(job.FilaNome, job.JobId));
            }

            return snapshot;
        }

        /// <summary>
        /// Monitora a chegada de novos jobs na fila do Spooler gerados após o snapshot.
        /// </summary>
        public ResultadoDeteccaoJobs DetectarNovosJobs(
            SnapshotFilas snapshot,
            string usuarioAtual,
            int viasEsperadas,
            int timeoutMs,
            int intervaloMs,
            Action<string> logProgresso = null,
            Func<bool> cancelamentoSolicitado = null)
        {
            var resultado = new ResultadoDeteccaoJobs();
            if (snapshot == null)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Snapshot de filas não fornecido para detecção de jobs.";
                return resultado;
            }

            viasEsperadas = Math.Max(1, viasEsperadas);
            timeoutMs = Math.Max(1000, timeoutMs);
            intervaloMs = Math.Max(100, intervaloMs);

            var inicio = DateTime.UtcNow;
            var jobsDetectadosMap = new Dictionary<string, InfoJob>(StringComparer.OrdinalIgnoreCase);

            while ((DateTime.UtcNow - inicio).TotalMilliseconds < timeoutMs)
            {
                if (cancelamentoSolicitado != null && cancelamentoSolicitado())
                {
                    resultado.Sucesso = false;
                    resultado.Mensagem = "Detecção de jobs cancelada pelo usuário.";
                    return resultado;
                }

                var jobsAtuais = _fonte.ObterTodosJobs()?.ToList() ?? new List<InfoJob>();

                foreach (var job in jobsAtuais)
                {
                    var chave = SnapshotFilas.CriarChave(job.FilaNome, job.JobId);

                    // 1. Não pode estar no snapshot inicial
                    if (snapshot.JobsPreexistentes.Contains(chave))
                    {
                        continue;
                    }

                    // 2. Horário de submissão posterior a T0 (com tolerância de 2 segundos para o relógio do spooler)
                    if (job.TimeJobSubmitted < snapshot.T0.AddSeconds(-2))
                    {
                        continue;
                    }

                    // 3. Usuário deve corresponder
                    if (!SubmitterCorrespondeAoUsuario(job.Submitter, usuarioAtual))
                    {
                        continue;
                    }

                    if (!jobsDetectadosMap.ContainsKey(chave))
                    {
                        jobsDetectadosMap[chave] = job;
                        logProgresso?.Invoke($"Job detectado: ID {job.JobId} na fila '{job.FilaNome}' (Documento: '{job.NomeDocumento}', Submitter: '{job.Submitter}').");
                    }
                }

                if (jobsDetectadosMap.Count >= viasEsperadas)
                {
                    break;
                }

                Thread.Sleep(intervaloMs);
            }

            resultado.JobsDetectados.AddRange(jobsDetectadosMap.Values);

            if (resultado.JobsDetectados.Count == 0)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "O CGEditor não enviou nada para a fila (impressora não selecionada, diálogo cancelado ou driver recusou).";
                return resultado;
            }

            if (resultado.JobsDetectados.Count < viasEsperadas)
            {
                resultado.Sucesso = true;
                resultado.TemAviso = true;
                var ids = string.Join(", ", resultado.JobsDetectados.Select(j => $"ID {j.JobId} ({j.FilaNome})"));
                resultado.Mensagem = $"AVISO: Apenas {resultado.JobsDetectados.Count} de {viasEsperadas} via(s) foram detectadas na fila ({ids}).";
                return resultado;
            }

            resultado.Sucesso = true;
            resultado.TemAviso = false;
            var idsOk = string.Join(", ", resultado.JobsDetectados.Select(j => $"ID {j.JobId} ({j.FilaNome})"));
            resultado.Mensagem = $"{viasEsperadas} job(s) detectado(s) na fila com sucesso ({idsOk}).";
            return resultado;
        }

        /// <summary>
        /// Acompanha os jobs detectados até a conclusão no spooler (entrega ou falha).
        /// </summary>
        public ResultadoAcompanhamentoJobs AcompanharConclusao(
            IEnumerable<InfoJob> jobsParaAcompanhar,
            int timeoutMs,
            int intervaloMs,
            Action<string> logProgresso = null,
            Func<bool> cancelamentoSolicitado = null)
        {
            var resultado = new ResultadoAcompanhamentoJobs();
            var listaJobs = jobsParaAcompanhar?.ToList() ?? new List<InfoJob>();

            if (listaJobs.Count == 0)
            {
                resultado.Sucesso = false;
                resultado.Mensagem = "Nenhum job para acompanhar.";
                return resultado;
            }

            timeoutMs = Math.Max(1000, timeoutMs);
            intervaloMs = Math.Max(100, intervaloMs);

            var inicio = DateTime.UtcNow;
            var pendentes = new Dictionary<string, InfoJob>(StringComparer.OrdinalIgnoreCase);
            var concluidos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var filasEnvolvidas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var job in listaJobs)
            {
                var chave = SnapshotFilas.CriarChave(job.FilaNome, job.JobId);
                pendentes[chave] = job;
                filasEnvolvidas.Add(job.FilaNome);
            }

            while (pendentes.Count > 0 && (DateTime.UtcNow - inicio).TotalMilliseconds < timeoutMs)
            {
                if (cancelamentoSolicitado != null && cancelamentoSolicitado())
                {
                    resultado.Sucesso = false;
                    resultado.Mensagem = "Acompanhamento de conclusão cancelado pelo usuário.";
                    return resultado;
                }

                var jobsSpooler = _fonte.ObterTodosJobs()?.ToList() ?? new List<InfoJob>();
                var chavesSpooler = new HashSet<string>(
                    jobsSpooler.Select(j => SnapshotFilas.CriarChave(j.FilaNome, j.JobId)),
                    StringComparer.OrdinalIgnoreCase);

                var chavesParaRemover = new List<string>();

                foreach (var kvp in pendentes)
                {
                    var chave = kvp.Key;
                    var jobRef = kvp.Value;

                    var jobAtual = jobsSpooler.FirstOrDefault(j =>
                        string.Equals(SnapshotFilas.CriarChave(j.FilaNome, j.JobId), chave, StringComparison.OrdinalIgnoreCase));

                    if (jobAtual != null)
                    {
                        // Job ainda está na fila: inspecionar flags
                        if (jobAtual.TemErro)
                        {
                            var flagErro = jobAtual.ObterFlagsAtivas();
                            resultado.Sucesso = false;
                            resultado.Mensagem = $"FALHA na fila: Job {jobAtual.JobId} na fila '{jobAtual.FilaNome}' reportou status de erro: [{flagErro}].";
                            ColetarFlagsFilas(filasEnvolvidas, resultado);
                            return resultado;
                        }

                        if (jobAtual.ConcluidoComSucesso)
                        {
                            concluidos[chave] = $"Job {jobAtual.JobId} concluído com flag [{jobAtual.ObterFlagsAtivas()}].";
                            chavesParaRemover.Add(chave);
                            logProgresso?.Invoke($"Job {jobAtual.JobId} concluído no Spooler.");
                        }
                    }
                    else
                    {
                        // Job sumiu da fila do spooler: foi entregue à impressora/porta sem erros ativos!
                        concluidos[chave] = $"Job {jobRef.JobId} processado e descarregado do spooler.";
                        chavesParaRemover.Add(chave);
                        logProgresso?.Invoke($"Job {jobRef.JobId} descarregado do spooler (entregue à porta/impressora).");
                    }
                }

                foreach (var chave in chavesParaRemover)
                {
                    pendentes.Remove(chave);
                }

                if (pendentes.Count == 0)
                {
                    break;
                }

                Thread.Sleep(intervaloMs);
            }

            ColetarFlagsFilas(filasEnvolvidas, resultado);

            // Verificar se alguma fila envolvida está em estado crítico de erro
            foreach (var filaNome in filasEnvolvidas)
            {
                var statusFila = _fonte.ObterStatusFila(filaNome);
                if (statusFila != null)
                {
                    if (statusFila.IsInError || statusFila.IsOffline || statusFila.IsOutOfPaper || statusFila.IsPaperJammed)
                    {
                        resultado.Sucesso = false;
                        resultado.Mensagem = $"FALHA na impressora '{filaNome}': Status da fila [{statusFila.ObterDescricaoFlags()}].";
                        return resultado;
                    }
                }
            }

            if (pendentes.Count > 0)
            {
                // Timeout com jobs ainda parados na fila
                resultado.Sucesso = true;
                resultado.TemAviso = true;
                var pendentesIds = string.Join(", ", pendentes.Values.Select(j => $"ID {j.JobId} ({j.FilaNome})"));
                resultado.Mensagem = $"AVISO: Job parado na fila (timeout de {timeoutMs / 1000}s atingido sem confirmação de término no spooler para: {pendentesIds}).";
                return resultado;
            }

            resultado.Sucesso = true;
            resultado.TemAviso = false;
            resultado.Mensagem = "Documento entregue à impressora com sucesso (todos os jobs concluídos no spooler).";
            return resultado;
        }

        private void ColetarFlagsFilas(IEnumerable<string> filas, ResultadoAcompanhamentoJobs resultado)
        {
            foreach (var fila in filas)
            {
                try
                {
                    var status = _fonte.ObterStatusFila(fila);
                    if (status != null)
                    {
                        resultado.FlagsFilas[fila] = status.ObterDescricaoFlags();
                    }
                }
                catch
                {
                    // Ignora falha na leitura individual de status da fila
                }
            }
        }

        public static bool SubmitterCorrespondeAoUsuario(string submitterJob, string usuarioAtual)
        {
            if (string.IsNullOrWhiteSpace(submitterJob) || string.IsNullOrWhiteSpace(usuarioAtual))
            {
                return true;
            }

            string nomePuro = submitterJob;
            int barra = submitterJob.LastIndexOf('\\');
            if (barra >= 0 && barra < submitterJob.Length - 1)
            {
                nomePuro = submitterJob.Substring(barra + 1);
            }

            return string.Equals(submitterJob, usuarioAtual, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(nomePuro, usuarioAtual, StringComparison.OrdinalIgnoreCase) ||
                   submitterJob.IndexOf(usuarioAtual, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
