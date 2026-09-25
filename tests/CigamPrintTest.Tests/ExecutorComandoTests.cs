using System;
using System.IO;
using CigamPrintTest.Config;
using CigamPrintTest.Execucao;
using Xunit;

namespace CigamPrintTest.Tests
{
    public class ExecutorComandoTests
    {
        [Fact]
        public void MontagemComando_PerfilCgEditor_DeveGerarExatamenteArgumentoEspecificado()
        {
            // Requisito obrigatório do CIGAM:
            // C:\Cigam\Laminort\CIGAM11\CGEditor.exe -VG: -A:C:\x\DANFE30000044312.rtf
            // Argumentos do perfil: -VG: -A:{arquivoSemAspas}
            var perfil = new PerfilImpressao
            {
                Nome = "CIGAM - DANFE (CGEditor)",
                Executavel = @"C:\Cigam\Laminort\CIGAM11\CGEditor.exe",
                Argumentos = "-VG: -A:{arquivoSemAspas}"
            };

            var caminhoRtf = @"C:\x\DANFE30000044312.rtf";
            var argumentosGerados = ExecutorComando.SubstituirPlaceholders(perfil.Argumentos, caminhoRtf);

            Assert.Equal("-VG: -A:C:\\x\\DANFE30000044312.rtf", argumentosGerados);
        }

        [Fact]
        public void Placeholders_ArquivoComEspacos_ComE_SemAspas()
        {
            var caminhoComEspaco = @"C:\Pastas do Cigam\DANFE 12345.rtf";

            // Com {arquivo} deve colocar aspas
            var argsComAspas = ExecutorComando.SubstituirPlaceholders("{arquivo}", caminhoComEspaco);
            Assert.Equal("\"" + caminhoComEspaco + "\"", argsComAspas);

            // Com {arquivoSemAspas} NÃO deve colocar aspas
            var argsSemAspas = ExecutorComando.SubstituirPlaceholders("{arquivoSemAspas}", caminhoComEspaco);
            Assert.Equal(caminhoComEspaco, argsSemAspas);
        }

        [Fact]
        public void Placeholders_Nome_NomeSemExtensao_Pasta_Usuario_Maquina_CodTeste()
        {
            var caminho = @"C:\Cigam\Relatorios\NotaFiscal_9988.rtf";
            var codTeste = "T20260925120000";

            var template = "FILE={nomeArquivo}|NAME={nomeSemExtensao}|DIR={pasta}|TEST={codTeste}|USER={usuario}|PC={maquina}";
            var resultado = ExecutorComando.SubstituirPlaceholders(template, caminho, codTeste);

            var esperado = $"FILE=NotaFiscal_9988.rtf|NAME=NotaFiscal_9988|DIR=C:\\Cigam\\Relatorios|TEST={codTeste}|USER={Environment.UserName}|PC={Environment.MachineName}";
            Assert.Equal(esperado, resultado);
        }

        [Fact]
        public void Placeholders_ExpansaoVariaveisDeAmbiente()
        {
            var template = "%USERNAME%_relatorio_{nomeArquivo}";
            var caminho = @"C:\x\teste.txt";

            var resultado = ExecutorComando.SubstituirPlaceholders(template, caminho);
            var usuario = Environment.UserName;

            Assert.Equal($"{usuario}_relatorio_teste.txt", resultado);
        }

        [Fact]
        public void ResolverPastaExecucao_QuandoVazio_RetornaPastaDoExecutavel()
        {
            var perfil = new PerfilImpressao
            {
                Nome = "Teste",
                Executavel = @"C:\Cigam\Laminort\CIGAM11\CGEditor.exe",
                PastaExecucao = ""
            };

            var pasta = ExecutorComando.ResolverPastaExecucao(perfil, @"C:\x\doc.rtf");
            Assert.Equal(@"C:\Cigam\Laminort\CIGAM11", pasta);
        }

        [Fact]
        public void ResolverPastaExecucao_QuandoConfigurada_RespeitaConfiguracao()
        {
            var perfil = new PerfilImpressao
            {
                Nome = "Teste",
                Executavel = @"C:\Windows\System32\notepad.exe",
                PastaExecucao = @"C:\Temp"
            };

            var pasta = ExecutorComando.ResolverPastaExecucao(perfil, @"C:\x\doc.rtf");
            Assert.Equal(@"C:\Temp", pasta);
        }

        [Fact]
        public void MontarComandoCompleto_FormataExecutavelEntreAspasComArgumentos()
        {
            var perfil = new PerfilImpressao
            {
                Nome = "Notepad",
                Executavel = @"C:\Windows\System32\notepad.exe",
                Argumentos = "/p {arquivo}"
            };

            var caminho = @"C:\Notas Fiscais\doc.txt";
            var comando = ExecutorComando.MontarComandoCompleto(perfil, caminho);

            Assert.Equal("\"C:\\Windows\\System32\\notepad.exe\" /p \"C:\\Notas Fiscais\\doc.txt\"", comando);
        }
    }
}
