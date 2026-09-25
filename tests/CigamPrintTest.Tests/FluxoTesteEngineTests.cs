using System;
using System.IO;
using CigamPrintTest.Config;
using CigamPrintTest.Execucao;
using CigamPrintTest.Modelo;
using Xunit;

namespace CigamPrintTest.Tests
{
    public class FluxoTesteEngineTests
    {
        [Fact]
        public void Execucao_ArquivoInexistente_DeveFalharEtapa2_ComErro2EmPortugues()
        {
            var config = ConfigLoader.DesserializarEValidar(ConfigLoader.ObterJsonPadrao());
            var fakeFilas = new FonteFilasFake();
            var engine = new FluxoTesteEngine(config, fakeFilas);

            var perfil = config.Perfis[0];
            var arquivoInexistente = @"C:\Caminho_Que_Nao_Existe_12345\danfe.rtf";

            var resultado = engine.Executar(perfil, arquivoInexistente, vias: 1, apenasAmbiente: false);

            Assert.NotNull(resultado);
            var etapa2 = resultado.ObterOuCriarEtapa(2, "ARQUIVO");
            Assert.Equal(StatusEtapa.Falha, etapa2.Status);
            Assert.Contains("Arquivo não encontrado", etapa2.Mensagem);
            Assert.Contains("ERROR_FILE_NOT_FOUND", etapa2.Mensagem);
            Assert.Contains("Camada 2 (Arquivo)", resultado.DiagnosticoFinal);
        }

        [Fact]
        public void Execucao_ExecutavelInexistente_DeveFalharEtapa3_ComMensagemClara()
        {
            var config = ConfigLoader.DesserializarEValidar(ConfigLoader.ObterJsonPadrao());
            var fakeFilas = new FonteFilasFake();
            var engine = new FluxoTesteEngine(config, fakeFilas);

            // Perfil apontando para executável inexistente
            var perfil = new PerfilImpressao
            {
                Nome = "CGEditor Inexistente",
                Executavel = @"C:\PastaInexistente\CGEditor.exe",
                Argumentos = "-VG: -A:{arquivoSemAspas}"
            };

            // Cria arquivo temporário real válido para passar a etapa 2
            var tempFile = Path.Combine(Path.GetTempPath(), $"teste_danfe_{Guid.NewGuid():N}.rtf");
            File.WriteAllText(tempFile, @"{\rtf1\ansi Teste DANFE}");

            try
            {
                var resultado = engine.Executar(perfil, tempFile, vias: 1, apenasAmbiente: false);

                Assert.NotNull(resultado);
                var etapa1 = resultado.ObterOuCriarEtapa(1, "AMBIENTE");
                var etapa2 = resultado.ObterOuCriarEtapa(2, "ARQUIVO");
                var etapa3 = resultado.ObterOuCriarEtapa(3, "VISUALIZADOR");

                Assert.Equal(StatusEtapa.OK, etapa1.Status);
                Assert.Equal(StatusEtapa.OK, etapa2.Status);
                Assert.Equal(StatusEtapa.Falha, etapa3.Status);
                Assert.Contains("Executável do visualizador não encontrado", etapa3.Mensagem);
                Assert.Contains("CGEditor", etapa3.Mensagem);
                Assert.Contains("Camada 3 (Visualizador)", resultado.DiagnosticoFinal);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }

        [Fact]
        public void Execucao_CaminhoComEspaco_SemAspas_GeraAvisoNaEtapa2_MasContinua()
        {
            var config = ConfigLoader.DesserializarEValidar(ConfigLoader.ObterJsonPadrao());
            var fakeFilas = new FonteFilasFake();
            var engine = new FluxoTesteEngine(config, fakeFilas);

            var perfil = config.Perfis[0]; // usa {arquivoSemAspas}

            var tempDir = Path.Combine(Path.GetTempPath(), $"Pasta Com Espaco {Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            var tempFile = Path.Combine(tempDir, "DANFE 1000.rtf");
            File.WriteAllText(tempFile, @"{\rtf1\ansi Conteudo}");

            try
            {
                var resultado = engine.Executar(perfil, tempFile, vias: 1, apenasAmbiente: true);

                var etapa2 = resultado.ObterOuCriarEtapa(2, "ARQUIVO");
                Assert.Equal(StatusEtapa.Aviso, etapa2.Status);
                Assert.Contains("o CIGAM passa este caminho sem aspas", etapa2.Mensagem, StringComparison.OrdinalIgnoreCase);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
        [Fact]
        public void DiagnosticoFinal_SomenteAmbiente_NaoAfirmaQueImprimiu()
        {
            var r = ResultadoTeste.CriarNovo(null, @"C:\x\teste.rtf", 1);
            r.ObterOuCriarEtapa(1, "AMBIENTE").Status = StatusEtapa.OK;
            r.ObterOuCriarEtapa(2, "ARQUIVO").Status = StatusEtapa.OK;

            var diag = r.AtualizarDiagnosticoFinal();

            Assert.Contains("Nenhuma impressão foi disparada", diag);
        }
    }
}
