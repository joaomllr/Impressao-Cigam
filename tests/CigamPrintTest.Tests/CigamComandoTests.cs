using CigamPrintTest.Diagnostico;
using CigamPrintTest.Modelo;
using System;
using System.IO;
using CigamPrintTest.Config;
using Xunit;

namespace CigamPrintTest.Tests
{
    public class CigamComandoTests
    {
        [Fact]
        public void RegraCigam_SemAspas_MontaExecutavelEArgumentosCorretamente()
        {
            // valor "%CIGAM_INSTAL%CGEditor.exe -VG: -A:", instal "C:\Cigam\ClienteX\CIGAM11", arquivo "C:\x\DANFE1.rtf"
            // -> executável "C:\Cigam\ClienteX\CIGAM11\CGEditor.exe", argumentos "-VG: -A:C:\x\DANFE1.rtf"
            var valor = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:";
            var instal = @"C:\Cigam\ClienteX\CIGAM11";
            var arquivo = @"C:\x\DANFE1.rtf";

            CigamComandoHelper.Resolver(valor, instal, arquivo, out var exe, out var args, out var linha);

            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\CGEditor.exe", exe);
            Assert.Equal(@"-VG: -A:C:\x\DANFE1.rtf", args);
            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\CGEditor.exe -VG: -A:C:\x\DANFE1.rtf", linha);
        }

        [Fact]
        public void RegraCigam_ComAspas_MontaExecutavelEArgumentosComAspaFinal()
        {
            // valor "\"%CIGAM_INSTAL%CGEditor.exe\" -A:\"", mesmo instal/arquivo
            // -> executável "C:\Cigam\ClienteX\CIGAM11\CGEditor.exe", argumentos "-A:\"C:\x\DANFE1.rtf\"" (aspa final acrescentada pela regra)
            var valor = "\"%CIGAM_INSTAL%CGEditor.exe\" -A:\"";
            var instal = @"C:\Cigam\ClienteX\CIGAM11";
            var arquivo = @"C:\x\DANFE1.rtf";

            CigamComandoHelper.Resolver(valor, instal, arquivo, out var exe, out var args, out var linha);

            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\CGEditor.exe", exe);
            Assert.Equal("-A:\"C:\\x\\DANFE1.rtf\"", args);
            Assert.Equal("\"C:\\Cigam\\ClienteX\\CIGAM11\\CGEditor.exe\" -A:\"C:\\x\\DANFE1.rtf\"", linha);
        }

        [Fact]
        public void NormalizarCigamInstal_SemBarraFinal_RecebeBarraFinal()
        {
            // cigamInstal sem barra final recebe "\"
            var instal = @"C:\Cigam\ClienteX\CIGAM11";
            var normalizado = CigamComandoHelper.NormalizarCigamInstal(instal);

            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\", normalizado);

            var comBarra = @"C:\Cigam\ClienteX\CIGAM11\";
            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\", CigamComandoHelper.NormalizarCigamInstal(comBarra));
        }

        [Fact]
        public void MetodoDecisao_ConfigSemConfigurado_AssistenteDeveSerExigido()
        {
            // config sem "configurado" -> assistente deve ser exigido (testar o método de decisão, não a UI)
            var config = new ConfigApp
            {
                Configurado = false,
                PerfilPadrao = "CIGAM - DANFE"
            };

            Assert.True(ConfigLoader.DeveExibirAssistente(config));

            var jsonSemConfigurado = @"{
  ""pastaLogs"": ""%LOCALAPPDATA%\\CigamPrintTest\\logs"",
  ""timeoutDeteccaoJobMs"": 60000,
  ""timeoutConclusaoJobMs"": 120000,
  ""intervaloPollingMs"": 500,
  ""perfilPadrao"": ""P1"",
  ""perfis"": [{ ""nome"": ""P1"", ""executavel"": ""notepad.exe"" }]
}";
            var configDesserializado = ConfigLoader.DesserializarEValidar(jsonSemConfigurado);
            Assert.False(configDesserializado.Configurado);
            Assert.True(ConfigLoader.DeveExibirAssistente(configDesserializado));
        }

        [Fact]
        public void MetodoDecisao_ConfigNuloOuInexistente_AssistenteDeveSerExigido()
        {
            Assert.True(ConfigLoader.DeveExibirAssistente(null));

            var fakePath = Path.Combine(Path.GetTempPath(), $"nao_existe_{Guid.NewGuid():N}.json");
            Assert.True(ConfigLoader.DeveExibirAssistente(fakePath));
        }

        [Fact]
        public void ConfigAntigo_ApenasComExecutavelEArgumentos_ContinuaValido()
        {
            // config antigo só com executavel/argumentos continua válido
            var jsonAntigo = @"{
  ""pastaLogs"": ""%LOCALAPPDATA%\\CigamPrintTest\\logs"",
  ""timeoutDeteccaoJobMs"": 60000,
  ""timeoutConclusaoJobMs"": 120000,
  ""intervaloPollingMs"": 500,
  ""perfilPadrao"": ""CIGAM - DANFE (CGEditor)"",
  ""perfis"": [
    {
      ""nome"": ""CIGAM - DANFE (CGEditor)"",
      ""executavel"": ""C:\\Cigam\\Laminort\\CIGAM11\\CGEditor.exe"",
      ""argumentos"": ""-VG: -A:{arquivoSemAspas}"",
      ""pastaExecucao"": """",
      ""esperarProcesso"": false,
      ""timeoutProcessoMs"": 60000,
      ""extensoesPermitidas"": ["".rtf""],
      ""pdfEsperado"": """"
    }
  ]
}";
            var config = ConfigLoader.DesserializarEValidar(jsonAntigo);
            Assert.NotNull(config);
            Assert.Single(config.Perfis);

            var p = config.Perfis[0];
            Assert.Equal("CIGAM - DANFE (CGEditor)", p.Nome);
            Assert.Equal(@"C:\Cigam\Laminort\CIGAM11\CGEditor.exe", p.Executavel);
            Assert.Equal("-VG: -A:{arquivoSemAspas}", p.Argumentos);
            Assert.True(string.IsNullOrEmpty(p.ComandoCigam));
        }

        [Fact]
        public void PerfilComComandoCigam_TemPrioridadeSobreExecutavelEArgumentos()
        {
            var perfil = new PerfilImpressao
            {
                Nome = "CIGAM - DANFE",
                ComandoCigam = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:",
                CigamInstal = @"C:\Cigam\ClienteX\CIGAM11\",
                Executavel = @"C:\OutroCaminho\Ignorado.exe",
                Argumentos = "-OutrosArgs"
            };

            var exeResolvido = Execucao.ExecutorComando.ResolverExecutavel(perfil, @"C:\x\DANFE1.rtf");
            var argsResolvidos = Execucao.ExecutorComando.ResolverArgumentos(perfil, @"C:\x\DANFE1.rtf");
            var comandoCompleto = Execucao.ExecutorComando.MontarComandoCompleto(perfil, @"C:\x\DANFE1.rtf");

            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\CGEditor.exe", exeResolvido);
            Assert.Equal(@"-VG: -A:C:\x\DANFE1.rtf", argsResolvidos);
            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\CGEditor.exe -VG: -A:C:\x\DANFE1.rtf", comandoCompleto);
        }
    
        [Fact]
        public void RelatorioCompleto_Cabecalho_ContemClienteECigamInstal()
        {
            var perfil = new PerfilImpressao
            {
                Nome = "CIGAM - DANFE",
                ComandoCigam = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:",
                CigamInstal = @"C:\Cigam\ClienteX\CIGAM11\"
            };

            var resultado = ResultadoTeste.CriarNovo(perfil, @"C:\x\DANFE1.rtf", 1);
            resultado.Cliente = "Cliente Empresa XYZ";
            resultado.CigamInstal = @"C:\Cigam\ClienteX\CIGAM11\";

            var relatorio = resultado.GerarRelatorioCompleto();

            Assert.Contains("Cliente:         Cliente Empresa XYZ", relatorio);
            Assert.Contains(@"%CIGAM_INSTAL%:  C:\Cigam\ClienteX\CIGAM11\", relatorio);
        }

        [Fact]
        public void FluxoAceite_AposSalvarAssistente_Etapa3MostraMesmoComandoDaPrevia()
        {
            var cliente = "Cliente Industrial S/A";
            var cigamInstal = @"C:\Cigam\ClienteX\CIGAM11";
            var valorConfig2128 = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:";
            var arquivoExemplo = CigamComandoHelper.ArquivoExemploPreview;

            // 1. Prévia calculada no assistente
            CigamComandoHelper.Resolver(valorConfig2128, cigamInstal, arquivoExemplo, out var exePrevia, out var argsPrevia, out var linhaPrevia);

            // 2. Monta config salva pelo assistente
            var config = new ConfigApp
            {
                Configurado = true,
                Cliente = cliente,
                CigamInstal = CigamComandoHelper.NormalizarCigamInstal(cigamInstal),
                PerfilPadrao = "CIGAM - DANFE",
                Perfis = new System.Collections.Generic.List<PerfilImpressao>
                {
                    new PerfilImpressao
                    {
                        Nome = "CIGAM - DANFE",
                        ComandoCigam = valorConfig2128,
                        CigamInstal = CigamComandoHelper.NormalizarCigamInstal(cigamInstal),
                        EsperarProcesso = false
                    },
                    new PerfilImpressao
                    {
                        Nome = "Teste do programa (Notepad)",
                        Executavel = @"C:\Windows\System32\notepad.exe",
                        Argumentos = "/p {arquivo}",
                        EsperarProcesso = false
                    }
                }
            };

            // 3. Serializa e recarrega
            var json = ConfigLoader.Serializar(config);
            var configRecarregada = ConfigLoader.DesserializarEValidar(json);

            Assert.True(configRecarregada.Configurado);
            Assert.Equal("CIGAM - DANFE", configRecarregada.PerfilPadrao);

            var perfilRecarregado = configRecarregada.Perfis[0];
            Assert.Equal("CIGAM - DANFE", perfilRecarregado.Nome);

            // 4. Comando gerado na Etapa 3 pelo ExecutorComando
            var comandoEtapa3 = Execucao.ExecutorComando.MontarComandoCompleto(perfilRecarregado, arquivoExemplo);

            // O comando da Etapa 3 deve ser idêntico à prévia do assistente
            Assert.Equal(linhaPrevia, comandoEtapa3);
            Assert.Equal(@"C:\Cigam\ClienteX\CIGAM11\CGEditor.exe -VG: -A:C:\exemplo\DANFE30000044312.rtf", comandoEtapa3);
        }


        [Fact]
        public void ArquivoInfo_ComandoCigamSemAspas_AlertaEspacosAtivado()
        {
            var pastaTemp = Path.Combine(Path.GetTempPath(), "Cigam Teste Espacos " + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pastaTemp);
            var arquivoComEspaco = Path.Combine(pastaTemp, "DANFE Exemplo 123.rtf");
            File.WriteAllText(arquivoComEspaco, @"{\rtf1 teste}");

            try
            {
                var perfilSemAspas = new PerfilImpressao
                {
                    Nome = "CIGAM - DANFE",
                    ComandoCigam = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:",
                    CigamInstal = @"C:\Cigam\ClienteX\",
                    ExtensoesPermitidas = new System.Collections.Generic.List<string> { ".rtf" }
                };

                var infoSemAspas = ArquivoInfo.Analisar(arquivoComEspaco, perfilSemAspas);
                Assert.True(infoSemAspas.TemEspacosNoCaminho);
                Assert.True(infoSemAspas.AlertaEspacosSemAspas);
                Assert.True(infoSemAspas.TemAviso);

                var perfilComAspas = new PerfilImpressao
                {
                    Nome = "CIGAM - DANFE",
                    ComandoCigam = @"""%CIGAM_INSTAL%CGEditor.exe"" -A:""",
                    CigamInstal = @"C:\Cigam\ClienteX\",
                    ExtensoesPermitidas = new System.Collections.Generic.List<string> { ".rtf" }
                };

                var infoComAspas = ArquivoInfo.Analisar(arquivoComEspaco, perfilComAspas);
                Assert.True(infoComAspas.TemEspacosNoCaminho);
                Assert.False(infoComAspas.AlertaEspacosSemAspas);
            }
            finally
            {
                if (Directory.Exists(pastaTemp))
                {
                    Directory.Delete(pastaTemp, true);
                }
            }
        }

        [Fact]
        public void Assistente_RemovePerfisCigamAntigos_AoSalvarNovo()
        {
            var config = new ConfigApp
            {
                Perfis = new System.Collections.Generic.List<PerfilImpressao>
                {
                    new PerfilImpressao
                    {
                        Nome = "CIGAM - DANFE (CGEditor)",
                        Executavel = @"C:\Cigam\Laminort\CIGAM11\CGEditor.exe",
                        Argumentos = "-VG: -A:{arquivoSemAspas}"
                    },
                    new PerfilImpressao
                    {
                        Nome = "Teste do programa (Notepad)",
                        Executavel = @"C:\Windows\System32\notepad.exe"
                    }
                }
            };

            var perfilDanfe = new PerfilImpressao
            {
                Nome = "CIGAM - DANFE",
                ComandoCigam = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:",
                CigamInstal = @"C:\Cigam\NovoCliente\"
            };
            config.Perfis.Insert(0, perfilDanfe);

            // Regra do assistente de remover perfis antigos
            config.Perfis.RemoveAll(p => !ReferenceEquals(p, perfilDanfe) &&
                p.Nome != null && p.Nome.StartsWith("CIGAM - DANFE", StringComparison.OrdinalIgnoreCase));

            Assert.Equal(2, config.Perfis.Count);
            Assert.Same(perfilDanfe, config.Perfis[0]);
            Assert.Equal("CIGAM - DANFE", config.Perfis[0].Nome);
            Assert.Equal("Teste do programa (Notepad)", config.Perfis[1].Nome);
            Assert.DoesNotContain(config.Perfis, p => p.Nome == "CIGAM - DANFE (CGEditor)");
        }
    }
}
