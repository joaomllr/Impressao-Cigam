using System;
using System.Collections.Generic;
using System.Linq;
using CigamPrintTest.Fila;
using Xunit;

namespace CigamPrintTest.Tests
{
    public class FonteFilasFake : IFonteFilas
    {
        public List<InfoImpressora> Impressoras { get; set; } = new List<InfoImpressora>();
        public List<InfoJob> Jobs { get; set; } = new List<InfoJob>();

        public IEnumerable<InfoImpressora> ObterImpressoras() => Impressoras;
        public IEnumerable<InfoJob> ObterTodosJobs() => Jobs;

        public InfoImpressora ObterStatusFila(string nomeFila)
        {
            return Impressoras.FirstOrDefault(i => string.Equals(i.Nome, nomeFila, StringComparison.OrdinalIgnoreCase))
                ?? new InfoImpressora { Nome = nomeFila, StatusTexto = "Pronta" };
        }
    }

    public class MonitorFilasTests
    {
        [Fact]
        public void Deteccao_JobAntigo_Ignorado_QuandoSubmetidoAntesDeT0()
        {
            var fake = new FonteFilasFake();
            var monitor = new MonitorFilas(fake);

            var t0 = DateTime.Now;
            var snapshot = new SnapshotFilas
            {
                T0 = t0
            };

            // Job antigo submetido 10 segundos antes de T0
            fake.Jobs.Add(new InfoJob
            {
                JobId = 101,
                FilaNome = "HP LaserJet",
                Submitter = "operador",
                TimeJobSubmitted = t0.AddSeconds(-10)
            });

            var resultado = monitor.DetectarNovosJobs(snapshot, "operador", viasEsperadas: 1, timeoutMs: 200, intervaloMs: 50);

            Assert.False(resultado.Sucesso);
            Assert.Empty(resultado.JobsDetectados);
            Assert.Contains("não enviou nada para a fila", resultado.Mensagem);
        }

        [Fact]
        public void Deteccao_JobPreexistenteNoSnapshot_Ignorado()
        {
            var fake = new FonteFilasFake();
            var monitor = new MonitorFilas(fake);

            var t0 = DateTime.Now;
            var snapshot = new SnapshotFilas
            {
                T0 = t0
            };
            snapshot.JobsPreexistentes.Add(SnapshotFilas.CriarChave("HP LaserJet", 202));

            // Job com ID 202 já estava no snapshot
            fake.Jobs.Add(new InfoJob
            {
                JobId = 202,
                FilaNome = "HP LaserJet",
                Submitter = "operador",
                TimeJobSubmitted = t0.AddSeconds(1)
            });

            var resultado = monitor.DetectarNovosJobs(snapshot, "operador", viasEsperadas: 1, timeoutMs: 200, intervaloMs: 50);

            Assert.False(resultado.Sucesso);
            Assert.Empty(resultado.JobsDetectados);
        }

        [Fact]
        public void Deteccao_JobDeOutroUsuario_Ignorado()
        {
            var fake = new FonteFilasFake();
            var monitor = new MonitorFilas(fake);

            var t0 = DateTime.Now;
            var snapshot = new SnapshotFilas { T0 = t0 };

            // Job de outro usuário ("usuario_diferente")
            fake.Jobs.Add(new InfoJob
            {
                JobId = 303,
                FilaNome = "HP LaserJet",
                Submitter = "usuario_diferente",
                TimeJobSubmitted = t0.AddSeconds(1)
            });

            var resultado = monitor.DetectarNovosJobs(snapshot, "operador_cigam", viasEsperadas: 1, timeoutMs: 200, intervaloMs: 50);

            Assert.False(resultado.Sucesso);
            Assert.Empty(resultado.JobsDetectados);
        }

        [Fact]
        public void Deteccao_UmJobPorVia_DetectaMultiplasViasComSucesso()
        {
            var fake = new FonteFilasFake();
            var monitor = new MonitorFilas(fake);

            var t0 = DateTime.Now;
            var snapshot = new SnapshotFilas { T0 = t0 };

            // Duas vias esperadas -> dois jobs novos para o usuário correto
            fake.Jobs.Add(new InfoJob
            {
                JobId = 401,
                FilaNome = "Zebra Termica",
                Submitter = "DOMINIO\\operador",
                TimeJobSubmitted = t0.AddSeconds(1)
            });
            fake.Jobs.Add(new InfoJob
            {
                JobId = 402,
                FilaNome = "Zebra Termica",
                Submitter = "operador",
                TimeJobSubmitted = t0.AddSeconds(2)
            });

            var resultado = monitor.DetectarNovosJobs(snapshot, "operador", viasEsperadas: 2, timeoutMs: 300, intervaloMs: 50);

            Assert.True(resultado.Sucesso);
            Assert.False(resultado.TemAviso);
            Assert.Equal(2, resultado.JobsDetectados.Count);
            Assert.Contains("2 job(s) detectado(s)", resultado.Mensagem);
        }

        [Fact]
        public void Deteccao_MenosJobsQueVias_RetornaAviso()
        {
            var fake = new FonteFilasFake();
            var monitor = new MonitorFilas(fake);

            var t0 = DateTime.Now;
            var snapshot = new SnapshotFilas { T0 = t0 };

            // Esperadas 3 vias, mas só chegou 1 job
            fake.Jobs.Add(new InfoJob
            {
                JobId = 501,
                FilaNome = "HP LaserJet",
                Submitter = "operador",
                TimeJobSubmitted = t0.AddSeconds(1)
            });

            var resultado = monitor.DetectarNovosJobs(snapshot, "operador", viasEsperadas: 3, timeoutMs: 200, intervaloMs: 50);

            Assert.True(resultado.Sucesso);
            Assert.True(resultado.TemAviso);
            Assert.Single(resultado.JobsDetectados);
            Assert.Contains("AVISO: Apenas 1 de 3 via(s)", resultado.Mensagem);
        }

        [Fact]
        public void Conclusao_JobQueSomeSemErro_ConsideradoEntregueComSucesso()
        {
            var fake = new FonteFilasFake();
            fake.Impressoras.Add(new InfoImpressora
            {
                Nome = "HP LaserJet",
                IsInError = false,
                IsOffline = false
            });

            var monitor = new MonitorFilas(fake);

            var job = new InfoJob
            {
                JobId = 601,
                FilaNome = "HP LaserJet",
                Submitter = "operador",
                TimeJobSubmitted = DateTime.Now
            };

            // O job sumiu da fila do Spooler (fake.Jobs está vazio) e a fila está sem erros
            fake.Jobs.Clear();

            var resultado = monitor.AcompanharConclusao(new[] { job }, timeoutMs: 200, intervaloMs: 50);

            Assert.True(resultado.Sucesso);
            Assert.False(resultado.TemAviso);
            Assert.Contains("entregue à impressora com sucesso", resultado.Mensagem, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Conclusao_JobComErro_RetornaFalhaComFlag()
        {
            var fake = new FonteFilasFake();
            var monitor = new MonitorFilas(fake);

            var job = new InfoJob
            {
                JobId = 701,
                FilaNome = "HP LaserJet",
                Submitter = "operador",
                IsInError = true,
                IsPaperOut = true
            };
            fake.Jobs.Add(job);

            var resultado = monitor.AcompanharConclusao(new[] { job }, timeoutMs: 200, intervaloMs: 50);

            Assert.False(resultado.Sucesso);
            Assert.Contains("FALHA na fila", resultado.Mensagem);
            Assert.Contains("PaperOut", resultado.Mensagem);
        }
    }
}
