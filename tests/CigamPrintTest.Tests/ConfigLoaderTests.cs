using System;
using System.IO;
using CigamPrintTest.Config;
using Xunit;

namespace CigamPrintTest.Tests
{
    public class ConfigLoaderTests
    {
        [Fact]
        public void ConfigPadrao_DesserializacaoEValidacao_DeveSerValido()
        {
            var jsonPadrao = ConfigLoader.ObterJsonPadrao();
            var config = ConfigLoader.DesserializarEValidar(jsonPadrao, "memoria.json");

            Assert.NotNull(config);
            Assert.Equal("%LOCALAPPDATA%\\CigamPrintTest\\logs", config.PastaLogs);
            Assert.Equal(60000, config.TimeoutDeteccaoJobMs);
            Assert.Equal(120000, config.TimeoutConclusaoJobMs);
            Assert.Equal(500, config.IntervaloPollingMs);
            Assert.Equal("CIGAM - DANFE (CGEditor)", config.PerfilPadrao);
            Assert.Equal(2, config.Perfis.Count);

            var perfilDanfe = config.Perfis[0];
            Assert.Equal("CIGAM - DANFE (CGEditor)", perfilDanfe.Nome);
            Assert.Equal(@"C:\Cigam\Laminort\CIGAM11\CGEditor.exe", perfilDanfe.Executavel);
            Assert.Equal("-VG: -A:{arquivoSemAspas}", perfilDanfe.Argumentos);
            Assert.Contains(".rtf", perfilDanfe.ExtensoesPermitidas);
            Assert.False(perfilDanfe.EsperarProcesso);

            var perfilNotepad = config.Perfis[1];
            Assert.Equal("Teste do programa (Notepad)", perfilNotepad.Nome);
            Assert.Equal(@"C:\Windows\System32\notepad.exe", perfilNotepad.Executavel);
            Assert.Equal("/p {arquivo}", perfilNotepad.Argumentos);
            Assert.Contains(".txt", perfilNotepad.ExtensoesPermitidas);
            Assert.True(perfilNotepad.EsperarProcesso);
        }

        [Fact]
        public void Carregar_ArquivoInexistente_DeveLancarConfigException()
        {
            var arquivoFalso = Path.Combine(Path.GetTempPath(), $"nao_existe_{Guid.NewGuid():N}.json");

            var ex = Assert.Throws<ConfigException>(() => ConfigLoader.Carregar(arquivoFalso));
            Assert.Equal("arquivo", ex.Campo);
            Assert.Contains("não encontrado", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Desserializar_JsonInvalido_DeveLancarConfigException()
        {
            var jsonQuebrado = "{ esta_sintaxe_nao_eh_json }";

            var ex = Assert.Throws<ConfigException>(() => ConfigLoader.DesserializarEValidar(jsonQuebrado));
            Assert.Equal("formato_json", ex.Campo);
        }

        [Fact]
        public void Desserializar_SemPastaLogs_DeveLancarConfigException()
        {
            var json = @"{
  ""pastaLogs"": """",
  ""timeoutDeteccaoJobMs"": 60000,
  ""timeoutConclusaoJobMs"": 120000,
  ""intervaloPollingMs"": 500,
  ""perfilPadrao"": ""P1"",
  ""perfis"": [{ ""nome"": ""P1"", ""executavel"": ""notepad.exe"" }]
}";

            var ex = Assert.Throws<ConfigException>(() => ConfigLoader.DesserializarEValidar(json));
            Assert.Equal("pastaLogs", ex.Campo);
        }

        [Fact]
        public void Desserializar_TimeoutInvalido_DeveLancarConfigException()
        {
            var json = @"{
  ""pastaLogs"": ""C:\\Logs"",
  ""timeoutDeteccaoJobMs"": -100,
  ""timeoutConclusaoJobMs"": 120000,
  ""intervaloPollingMs"": 500,
  ""perfilPadrao"": ""P1"",
  ""perfis"": [{ ""nome"": ""P1"", ""executavel"": ""notepad.exe"" }]
}";

            var ex = Assert.Throws<ConfigException>(() => ConfigLoader.DesserializarEValidar(json));
            Assert.Equal("timeoutDeteccaoJobMs", ex.Campo);
        }

        [Fact]
        public void Desserializar_PerfilPadraoInexistente_DeveLancarConfigException()
        {
            var json = @"{
  ""pastaLogs"": ""C:\\Logs"",
  ""timeoutDeteccaoJobMs"": 60000,
  ""timeoutConclusaoJobMs"": 120000,
  ""intervaloPollingMs"": 500,
  ""perfilPadrao"": ""PerfilQueNaoExiste"",
  ""perfis"": [
    { ""nome"": ""P1"", ""executavel"": ""notepad.exe"" }
  ]
}";

            var ex = Assert.Throws<ConfigException>(() => ConfigLoader.DesserializarEValidar(json));
            Assert.Equal("perfilPadrao", ex.Campo);
            Assert.Contains("PerfilQueNaoExiste", ex.Message);
        }

        [Fact]
        public void CriarConfigPadrao_GravaArquivoValidoNoDisco()
        {
            var tempFile = Path.Combine(Path.GetTempPath(), $"cfg_teste_{Guid.NewGuid():N}.json");

            try
            {
                ConfigLoader.CriarConfigPadrao(tempFile);
                Assert.True(File.Exists(tempFile));

                var config = ConfigLoader.Carregar(tempFile);
                Assert.NotNull(config);
                Assert.Equal("CIGAM - DANFE (CGEditor)", config.PerfilPadrao);
            }
            finally
            {
                if (File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }
        }
    }
}
