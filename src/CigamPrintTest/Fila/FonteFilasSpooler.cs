using System;
using System.Collections.Generic;
using System.Linq;
using System.Printing;

namespace CigamPrintTest.Fila
{
    /// <summary>
    /// Implementação de IFonteFilas que consulta o Spooler do Windows via System.Printing.
    /// </summary>
    public class FonteFilasSpooler : IFonteFilas
    {
        public IEnumerable<InfoImpressora> ObterImpressoras()
        {
            var resultado = new List<InfoImpressora>();
            string nomePadrao = ObterNomeImpressoraPadrao();

            try
            {
                using (var server = new LocalPrintServer())
                {
                    var tiposEnum = new[]
                    {
                        EnumeratedPrintQueueTypes.Local,
                        EnumeratedPrintQueueTypes.Connections,
                        EnumeratedPrintQueueTypes.TerminalServer
                    };

                    PrintQueueCollection filas = null;
                    try
                    {
                        filas = server.GetPrintQueues(tiposEnum);
                    }
                    catch
                    {
                        // Fallback: obter sem flags específicas se der erro de enumeração
                        try
                        {
                            filas = server.GetPrintQueues();
                        }
                        catch
                        {
                            filas = null;
                        }
                    }

                    if (filas != null)
                    {
                        foreach (PrintQueue queue in filas)
                        {
                            try
                            {
                                queue.Refresh();
                                var info = MapearFila(queue, nomePadrao);
                                resultado.Add(info);
                            }
                            catch
                            {
                                // Fila inacessível ou sem permissão; ignorar individualmente
                            }
                        }
                    }
                }
            }
            catch
            {
                // Spooler indisponível ou inacessível
            }

            return resultado;
        }

        public IEnumerable<InfoJob> ObterTodosJobs()
        {
            var resultado = new List<InfoJob>();

            try
            {
                using (var server = new LocalPrintServer())
                {
                    var tiposEnum = new[]
                    {
                        EnumeratedPrintQueueTypes.Local,
                        EnumeratedPrintQueueTypes.Connections,
                        EnumeratedPrintQueueTypes.TerminalServer
                    };

                    PrintQueueCollection filas = null;
                    try
                    {
                        filas = server.GetPrintQueues(tiposEnum);
                    }
                    catch
                    {
                        try
                        {
                            filas = server.GetPrintQueues();
                        }
                        catch
                        {
                            filas = null;
                        }
                    }

                    if (filas != null)
                    {
                        foreach (PrintQueue queue in filas)
                        {
                            try
                            {
                                queue.Refresh();
                                PrintJobInfoCollection jobs = null;
                                try
                                {
                                    jobs = queue.GetPrintJobInfoCollection();
                                }
                                catch
                                {
                                    continue;
                                }

                                if (jobs != null)
                                {
                                    foreach (PrintSystemJobInfo job in jobs)
                                    {
                                        try
                                        {
                                            job.Refresh();
                                            resultado.Add(MapearJob(job, queue.FullName));
                                        }
                                        catch
                                        {
                                            // Job individual pode ter sido deletado durante a iteração
                                        }
                                    }
                                }
                            }
                            catch
                            {
                                // Erro na fila individual
                            }
                        }
                    }
                }
            }
            catch
            {
                // Spooler indisponível
            }

            return resultado;
        }

        public InfoImpressora ObterStatusFila(string nomeFila)
        {
            if (string.IsNullOrWhiteSpace(nomeFila))
            {
                return null;
            }

            try
            {
                using (var server = new LocalPrintServer())
                {
                    var queue = server.GetPrintQueue(nomeFila);
                    if (queue != null)
                    {
                        queue.Refresh();
                        return MapearFila(queue, ObterNomeImpressoraPadrao());
                    }
                }
            }
            catch
            {
                // Fila não encontrada ou sem permissão
            }

            return null;
        }

        private static string ObterNomeImpressoraPadrao()
        {
            try
            {
                using (var queue = LocalPrintServer.GetDefaultPrintQueue())
                {
                    return queue?.FullName;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static InfoImpressora MapearFila(PrintQueue queue, string nomePadrao)
        {
            string nome = queue.FullName ?? queue.Name ?? "Impressora sem nome";
            string driver = queue.QueueDriver?.Name ?? "N/D";
            string porta = queue.QueuePort?.Name ?? "N/D";

            bool ehPadrao = !string.IsNullOrEmpty(nomePadrao) &&
                            string.Equals(nome, nomePadrao, StringComparison.OrdinalIgnoreCase);

            bool ehRedirecionada = (!string.IsNullOrEmpty(porta) && (porta.StartsWith("TS", StringComparison.OrdinalIgnoreCase) || porta.StartsWith("RDP", StringComparison.OrdinalIgnoreCase))) ||
                                   nome.IndexOf("redirecionad", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   nome.IndexOf("redirected", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   nome.IndexOf("em sessão", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                   nome.IndexOf("in session", StringComparison.OrdinalIgnoreCase) >= 0;

            var info = new InfoImpressora
            {
                Nome = nome,
                Driver = driver,
                Porta = porta,
                EhPadrao = ehPadrao,
                RedirecionadaRdp = ehRedirecionada,
                IsOffline = queue.IsOffline,
                IsInError = queue.IsInError,
                IsOutOfPaper = queue.IsOutOfPaper,
                IsPaperJammed = queue.IsPaperJammed,
                IsPaused = queue.IsPaused,
                IsDoorOpened = queue.IsDoorOpened,
                NeedUserIntervention = queue.NeedUserIntervention,
                IsNotAvailable = queue.IsNotAvailable,
                NumberOfJobs = queue.NumberOfJobs
            };

            info.StatusTexto = info.ObterDescricaoFlags();
            return info;
        }

        private static InfoJob MapearJob(PrintSystemJobInfo job, string filaNome)
        {
            var status = job.JobStatus;

            var info = new InfoJob
            {
                JobId = job.JobIdentifier,
                FilaNome = filaNome,
                NomeDocumento = job.Name ?? string.Empty,
                Submitter = job.Submitter ?? string.Empty,
                TimeJobSubmitted = job.TimeJobSubmitted,
                IsCompleted = status.HasFlag(PrintJobStatus.Completed),
                IsPrinted = status.HasFlag(PrintJobStatus.Printed),
                IsInError = status.HasFlag(PrintJobStatus.Error),
                IsOffline = status.HasFlag(PrintJobStatus.Offline),
                IsPaperOut = status.HasFlag(PrintJobStatus.PaperOut),
                IsUserIntervention = status.HasFlag(PrintJobStatus.UserIntervention),
                IsBlocked = status.HasFlag(PrintJobStatus.Blocked),
                IsDeleted = status.HasFlag(PrintJobStatus.Deleted),
                IsPaused = status.HasFlag(PrintJobStatus.Paused),
                IsPrinting = status.HasFlag(PrintJobStatus.Printing),
                IsSpooling = status.HasFlag(PrintJobStatus.Spooling),
                StatusTexto = status.ToString()
            };

            return info;
        }
    }
}
