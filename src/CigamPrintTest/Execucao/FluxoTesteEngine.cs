using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using CigamPrintTest.Config;
using CigamPrintTest.Diagnostico;
using CigamPrintTest.Fila;
using CigamPrintTest.Modelo;

namespace CigamPrintTest.Execucao
{
    public class FluxoTesteEngine
    {
        private readonly ConfigApp _config;
        private readonly IFonteFilas _fonteFilas;

        public FluxoTesteEngine(ConfigApp config, IFonteFilas fonteFilas = null)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _fonteFilas = fonteFilas ?? new FonteFilasSpooler();
        }

        public ResultadoTeste Executar(
            PerfilImpressao perfil,
            string caminhoArquivo,
            int vias,
            bool apenasAmbiente,
            Action<EtapaTeste> onEtapaAtualizada = null,
            Action<string> onLogMensagem = null,
            Func<bool> cancelamentoSolicitado = null)
        {
            var resultado = ResultadoTeste.CriarNovo(perfil, caminhoArquivo, vias);
            resultado.Cliente = _config.Cliente ?? string.Empty;
            resultado.CigamInstal = _config.CigamInstal ?? string.Empty;

            // =========================================================================
            // ETAPA 1: AMBIENTE
            // =========================================================================
            var etapa1 = resultado.ObterOuCriarEtapa(1, "AMBIENTE");
            var sw1 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa1);

            AmbienteInfo ambiente = null;
            try
            {
                ambiente = AmbienteInfo.Coletar(_config.PastaLogs);
                resultado.Ambiente = ambiente;

                if (!ambiente.TempGravavel)
                {
                    etapa1.Status = StatusEtapa.Falha;
                    etapa1.Mensagem = $"Diretório temporário (TEMP: '{ambiente.TempPath}') não gravável: {ambiente.MensagemErroTemp}";
                }
                else if (!ambiente.PastaLogsGravavel)
                {
                    etapa1.Status = StatusEtapa.Falha;
                    etapa1.Mensagem = $"Pasta de logs ('{ambiente.PastaLogsResolvida}') não gravável: {ambiente.MensagemErroLogs}";
                }
                else
                {
                    etapa1.Status = StatusEtapa.OK;
                    etapa1.Mensagem = $"Usuário: {ambiente.Dominio}\\{ambiente.Usuario} | Máquina: {ambiente.Maquina} | Sessão: {ambiente.SessionName} {(ambiente.EhRdp ? "(RDP)" : "(Console)")} | Processo {ambiente.ProcessoBitness} (SO: {ambiente.SistemaOperacionalBitness}) | TEMP e Logs graváveis.";
                }
            }
            catch (Exception ex)
            {
                etapa1.Status = StatusEtapa.Falha;
                etapa1.Mensagem = $"Erro ao coletar diagnóstico do ambiente: {ex.Message}";
            }
            finally
            {
                sw1.Stop();
                etapa1.Duracao = sw1.Elapsed;
                onEtapaAtualizada?.Invoke(etapa1);
            }

            if (etapa1.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 2: ARQUIVO
            // =========================================================================
            var etapa2 = resultado.ObterOuCriarEtapa(2, "ARQUIVO");
            var sw2 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa2);

            ArquivoInfo arquivoInfo = null;
            try
            {
                arquivoInfo = ArquivoInfo.Analisar(caminhoArquivo, perfil);
                resultado.TamanhoArquivoBytes = arquivoInfo.TamanhoBytes;

                if (!arquivoInfo.Sucesso)
                {
                    etapa2.Status = StatusEtapa.Falha;
                    etapa2.Mensagem = arquivoInfo.MensagemStatus;
                }
                else if (arquivoInfo.TemAviso)
                {
                    etapa2.Status = StatusEtapa.Aviso;
                    etapa2.Mensagem = arquivoInfo.MensagemStatus;
                }
                else
                {
                    etapa2.Status = StatusEtapa.OK;
                    etapa2.Mensagem = arquivoInfo.MensagemStatus;
                }
            }
            catch (Exception ex)
            {
                etapa2.Status = StatusEtapa.Falha;
                etapa2.Mensagem = $"Erro inesperado ao validar arquivo: {ex.Message}";
            }
            finally
            {
                sw2.Stop();
                etapa2.Duracao = sw2.Elapsed;
                onEtapaAtualizada?.Invoke(etapa2);
            }

            if (etapa2.Status == StatusEtapa.Falha || apenasAmbiente)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 3: VISUALIZADOR
            // =========================================================================
            var etapa3 = resultado.ObterOuCriarEtapa(3, "VISUALIZADOR");
            var sw3 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa3);

            string executavel = string.Empty;
            string comandoFinal = string.Empty;
            string pastaExecucao = string.Empty;

            try
            {
                executavel = ExecutorComando.ResolverExecutavel(perfil, caminhoArquivo, resultado.CodTeste);
                pastaExecucao = ExecutorComando.ResolverPastaExecucao(perfil, caminhoArquivo, resultado.CodTeste);
                comandoFinal = ExecutorComando.MontarComandoCompleto(perfil, caminhoArquivo, resultado.CodTeste, 1);

                resultado.ComandoFinal = comandoFinal;
                resultado.PastaExecucaoFinal = pastaExecucao;

                if (!File.Exists(executavel))
                {
                    etapa3.Status = StatusEtapa.Falha;
                    etapa3.Mensagem = $"Executável do visualizador não encontrado: '{executavel}' (perfil '{perfil?.Nome}'). Verifique se o CGEditor ou o programa configurado está instalado nesta estação.";
                }
                else
                {
                    etapa3.Status = StatusEtapa.OK;
                    etapa3.Mensagem = $"Visualizador pronto. Linha de comando que será executada: {comandoFinal}";
                }
            }
            catch (Exception ex)
            {
                etapa3.Status = StatusEtapa.Falha;
                etapa3.Mensagem = $"Falha ao validar executável do visualizador: {ex.Message}";
            }
            finally
            {
                sw3.Stop();
                etapa3.Duracao = sw3.Elapsed;
                onEtapaAtualizada?.Invoke(etapa3);
            }

            if (etapa3.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 4: SNAPSHOT DAS FILAS
            // =========================================================================
            var etapa4 = resultado.ObterOuCriarEtapa(4, "SNAPSHOT DAS FILAS");
            var sw4 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa4);

            MonitorFilas monitor = null;
            SnapshotFilas snapshot = null;

            try
            {
                monitor = new MonitorFilas(_fonteFilas);
                snapshot = monitor.CriarSnapshot();

                etapa4.Status = StatusEtapa.OK;
                etapa4.Mensagem = $"Snapshot concluído em T0={snapshot.T0:HH:mm:ss.fff}. {snapshot.TotalFilasMonitoradas} fila(s) monitorada(s) e {snapshot.JobsPreexistentes.Count} job(s) preexistente(s) mapeado(s).";
            }
            catch (Exception ex)
            {
                etapa4.Status = StatusEtapa.Falha;
                etapa4.Mensagem = $"Falha ao capturar snapshot das filas de impressão: {ex.Message}";
            }
            finally
            {
                sw4.Stop();
                etapa4.Duracao = sw4.Elapsed;
                onEtapaAtualizada?.Invoke(etapa4);
            }

            if (etapa4.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 5: EXECUÇÃO
            // =========================================================================
            var etapa5 = resultado.ObterOuCriarEtapa(5, "EXECUÇÃO");
            var sw5 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa5);

            try
            {
                for (int via = 1; via <= vias; via++)
                {
                    if (cancelamentoSolicitado != null && cancelamentoSolicitado())
                    {
                        etapa5.Status = StatusEtapa.Falha;
                        etapa5.Mensagem = "Execução cancelada pelo usuário.";
                        break;
                    }

                    var resExec = ExecutorComando.Executar(perfil, caminhoArquivo, resultado.CodTeste, via, onLogMensagem);
                    if (!resExec.Sucesso)
                    {
                        etapa5.Status = StatusEtapa.Falha;
                        etapa5.Mensagem = $"Falha na via {via} de {vias}: {resExec.Mensagem}";
                        break;
                    }
                }

                if (etapa5.Status != StatusEtapa.Falha)
                {
                    etapa5.Status = StatusEtapa.OK;
                    etapa5.Mensagem = vias > 1
                        ? $"{vias} vias disparadas com sucesso pelo comando do sistema operacional."
                        : $"Processo disparado com sucesso pelo comando do sistema operacional.";
                }
            }
            catch (Exception ex)
            {
                etapa5.Status = StatusEtapa.Falha;
                etapa5.Mensagem = $"Falha crítica ao disparar processo: {ex.Message}";
            }
            finally
            {
                sw5.Stop();
                etapa5.Duracao = sw5.Elapsed;
                onEtapaAtualizada?.Invoke(etapa5);
            }

            if (etapa5.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 6: PDF (OPCIONAL)
            // =========================================================================
            var etapa6 = resultado.ObterOuCriarEtapa(6, "PDF");
            var sw6 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa6);

            try
            {
                if (string.IsNullOrWhiteSpace(perfil?.PdfEsperado))
                {
                    etapa6.Status = StatusEtapa.NaoVerificada;
                    etapa6.Mensagem = "Não configurado para este perfil (etapa ignorada).";
                }
                else
                {
                    var pdfEsperado = ExecutorComando.SubstituirPlaceholders(perfil.PdfEsperado, caminhoArquivo, resultado.CodTeste);
                    onLogMensagem?.Invoke($"Aguardando geração do PDF intermediário: '{pdfEsperado}'...");

                    int timeoutPdf = perfil.TimeoutProcessoMs > 0 ? perfil.TimeoutProcessoMs : 30000;
                    var inicioPdf = DateTime.UtcNow;
                    long ultimoTamanho = -1;
                    int leiturasEstaveis = 0;
                    bool pdfOk = false;

                    while ((DateTime.UtcNow - inicioPdf).TotalMilliseconds < timeoutPdf)
                    {
                        if (cancelamentoSolicitado != null && cancelamentoSolicitado())
                        {
                            break;
                        }

                        if (File.Exists(pdfEsperado))
                        {
                            try
                            {
                                var fi = new FileInfo(pdfEsperado);
                                if (fi.Length > 0 && fi.Length == ultimoTamanho)
                                {
                                    leiturasEstaveis++;
                                    if (leiturasEstaveis >= 2)
                                    {
                                        pdfOk = true;
                                        break;
                                    }
                                }
                                else
                                {
                                    ultimoTamanho = fi.Length;
                                    leiturasEstaveis = 0;
                                }
                            }
                            catch
                            {
                                // Arquivo ainda pode estar sendo gravado pelo conversor
                            }
                        }

                        Thread.Sleep(500);
                    }

                    if (pdfOk)
                    {
                        etapa6.Status = StatusEtapa.OK;
                        etapa6.Mensagem = $"PDF gerado com êxito ('{pdfEsperado}', tamanho estável de {ultimoTamanho:N0} bytes).";
                    }
                    else
                    {
                        etapa6.Status = StatusEtapa.Falha;
                        etapa6.Mensagem = $"O visualizador/conversor não gerou o PDF ('{pdfEsperado}') — problema fora da impressora.";
                    }
                }
            }
            catch (Exception ex)
            {
                etapa6.Status = StatusEtapa.Falha;
                etapa6.Mensagem = $"Falha ao verificar PDF esperado: {ex.Message}";
            }
            finally
            {
                sw6.Stop();
                etapa6.Duracao = sw6.Elapsed;
                onEtapaAtualizada?.Invoke(etapa6);
            }

            if (etapa6.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 7: DETECÇÃO DO(S) JOB(S)
            // =========================================================================
            var etapa7 = resultado.ObterOuCriarEtapa(7, "DETECÇÃO DO(S) JOB(S)");
            var sw7 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa7);

            ResultadoDeteccaoJobs resDet = null;
            try
            {
                onLogMensagem?.Invoke($"Iniciando detecção de jobs no spooler (timeout: {_config.TimeoutDeteccaoJobMs} ms)...");
                resDet = monitor.DetectarNovosJobs(
                    snapshot,
                    resultado.Ambiente?.Usuario ?? Environment.UserName,
                    vias,
                    _config.TimeoutDeteccaoJobMs,
                    _config.IntervaloPollingMs,
                    onLogMensagem,
                    cancelamentoSolicitado);

                resultado.JobsDetectados.AddRange(resDet.JobsDetectados);

                if (!resDet.Sucesso)
                {
                    etapa7.Status = StatusEtapa.Falha;
                    etapa7.Mensagem = resDet.Mensagem;
                }
                else if (resDet.TemAviso)
                {
                    etapa7.Status = StatusEtapa.Aviso;
                    etapa7.Mensagem = resDet.Mensagem;
                }
                else
                {
                    etapa7.Status = StatusEtapa.OK;
                    etapa7.Mensagem = resDet.Mensagem;
                }
            }
            catch (Exception ex)
            {
                etapa7.Status = StatusEtapa.Falha;
                etapa7.Mensagem = $"Falha durante detecção de jobs na fila: {ex.Message}";
            }
            finally
            {
                sw7.Stop();
                etapa7.Duracao = sw7.Elapsed;
                onEtapaAtualizada?.Invoke(etapa7);
            }

            if (etapa7.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 8: CONCLUSÃO
            // =========================================================================
            var etapa8 = resultado.ObterOuCriarEtapa(8, "CONCLUSÃO");
            var sw8 = Stopwatch.StartNew();
            onEtapaAtualizada?.Invoke(etapa8);

            try
            {
                onLogMensagem?.Invoke($"Acompanhando conclusão dos jobs no spooler (timeout: {_config.TimeoutConclusaoJobMs} ms)...");
                var resConc = monitor.AcompanharConclusao(
                    resDet.JobsDetectados,
                    _config.TimeoutConclusaoJobMs,
                    _config.IntervaloPollingMs,
                    onLogMensagem,
                    cancelamentoSolicitado);

                foreach (var kvp in resConc.FlagsFilas)
                {
                    resultado.FlagsFilas[kvp.Key] = kvp.Value;
                }

                if (!resConc.Sucesso)
                {
                    etapa8.Status = StatusEtapa.Falha;
                    etapa8.Mensagem = resConc.Mensagem;
                }
                else if (resConc.TemAviso)
                {
                    etapa8.Status = StatusEtapa.Aviso;
                    etapa8.Mensagem = resConc.Mensagem;
                }
                else
                {
                    etapa8.Status = StatusEtapa.OK;
                    etapa8.Mensagem = resConc.Mensagem;
                }
            }
            catch (Exception ex)
            {
                etapa8.Status = StatusEtapa.Falha;
                etapa8.Mensagem = $"Falha ao acompanhar conclusão dos jobs: {ex.Message}";
            }
            finally
            {
                sw8.Stop();
                etapa8.Duracao = sw8.Elapsed;
                onEtapaAtualizada?.Invoke(etapa8);
            }

            if (etapa8.Status == StatusEtapa.Falha)
            {
                resultado.AtualizarDiagnosticoFinal();
                return resultado;
            }

            // =========================================================================
            // ETAPA 9: CONFIRMAÇÃO VISUAL (Inicialização)
            // =========================================================================
            var etapa9 = resultado.ObterOuCriarEtapa(9, "CONFIRMAÇÃO VISUAL");
            etapa9.Status = StatusEtapa.Pendente;
            etapa9.Mensagem = "Documento entregue ao spooler. Aguardando confirmação do usuário se saiu no papel físico.";
            onEtapaAtualizada?.Invoke(etapa9);

            resultado.AtualizarDiagnosticoFinal();
            return resultado;
        }
    }
}
