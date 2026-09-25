using CigamPrintTest.Erros;
using Xunit;

namespace CigamPrintTest.Tests
{
    public class CatalogoErrosTests
    {
        [Theory]
        [InlineData(2, "Arquivo não encontrado", "ERROR_FILE_NOT_FOUND")]
        [InlineData(3, "Caminho não encontrado", "ERROR_PATH_NOT_FOUND")]
        [InlineData(5, "Acesso negado", "ERROR_ACCESS_DENIED")]
        [InlineData(32, "Arquivo em uso", "ERROR_SHARING_VIOLATION")]
        [InlineData(193, "Executável inválido", "ERROR_BAD_EXE_FORMAT")]
        [InlineData(740, "Operação exige elevação", "ERROR_ELEVATION_REQUIRED")]
        [InlineData(1722, "Servidor de impressão RPC indisponível", "RPC_S_SERVER_UNAVAILABLE")]
        [InlineData(1801, "Nome de impressora inválido", "ERROR_INVALID_PRINTER_NAME")]
        public void ObterErro_CodigosConhecidos_RetornaTituloEAcaoSugerida(int codigo, string parteTituloEsperada, string constanteEsperada)
        {
            var erro = CatalogoErros.ObterErro(codigo);

            Assert.Equal(codigo, erro.Codigo);
            Assert.Contains(parteTituloEsperada, erro.Titulo);
            Assert.Contains(constanteEsperada, erro.Titulo);
            Assert.False(string.IsNullOrWhiteSpace(erro.AcaoSugerida));
            Assert.False(string.IsNullOrWhiteSpace(erro.Descricao));
        }

        [Fact]
        public void ObterErro_1801_MencionaImpressoraRedirecionadaRdp()
        {
            var erro = CatalogoErros.ObterErro(1801);

            Assert.Contains("RDP", erro.AcaoSugerida);
            Assert.Contains("redirecionad", erro.AcaoSugerida, System.StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void ObterErro_CodigoDesconhecido_RetornaMensagemGenericaComCodigo()
        {
            var erro = CatalogoErros.ObterErro(99999);

            Assert.Equal(99999, erro.Codigo);
            Assert.Contains("99999", erro.Titulo);
            Assert.False(string.IsNullOrWhiteSpace(erro.AcaoSugerida));
        }
    }
}
