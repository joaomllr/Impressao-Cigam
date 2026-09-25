using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CigamPrintTest.Erros
{
    public class ErroDiagnostico
    {
        public int Codigo { get; set; }
        public string Titulo { get; set; }
        public string Descricao { get; set; }
        public string AcaoSugerida { get; set; }

        public override string ToString()
        {
            return $"{Titulo} (Código: {Codigo}). {Descricao} Ação sugerida: {AcaoSugerida}";
        }
    }

    public static class CatalogoErros
    {
        public static ErroDiagnostico ObterErro(int codigoWin32)
        {
            switch (codigoWin32)
            {
                case 2:
                    return new ErroDiagnostico
                    {
                        Codigo = 2,
                        Titulo = "Arquivo não encontrado (ERROR_FILE_NOT_FOUND)",
                        Descricao = "O arquivo especificado não existe ou o caminho informado está incorreto.",
                        AcaoSugerida = "Verifique se o caminho do arquivo está correto e se o arquivo não foi movido, renomeado ou excluído."
                    };

                case 3:
                    return new ErroDiagnostico
                    {
                        Codigo = 3,
                        Titulo = "Caminho não encontrado (ERROR_PATH_NOT_FOUND)",
                        Descricao = "O diretório ou caminho de pastas especificado não foi encontrado.",
                        AcaoSugerida = "Verifique se todas as pastas do caminho existem e se a unidade de disco está montada e acessível."
                    };

                case 5:
                    return new ErroDiagnostico
                    {
                        Codigo = 5,
                        Titulo = "Acesso negado (ERROR_ACCESS_DENIED)",
                        Descricao = "O usuário atual não possui permissões suficientes para acessar o arquivo, pasta ou impressora.",
                        AcaoSugerida = "Verifique as permissões de leitura/gravação NTFS no arquivo/pasta e a permissão de impressão na fila para o usuário atual."
                    };

                case 32:
                    return new ErroDiagnostico
                    {
                        Codigo = 32,
                        Titulo = "Arquivo em uso (ERROR_SHARING_VIOLATION)",
                        Descricao = "O arquivo está sendo utilizado por outro processo ou aplicativo no momento.",
                        AcaoSugerida = "Feche outros programas que possam estar com o arquivo aberto (como o CIGAM, Word, WordPad, visualizador de PDF ou antivírus) e tente novamente."
                    };

                case 193:
                    return new ErroDiagnostico
                    {
                        Codigo = 193,
                        Titulo = "Executável inválido (ERROR_BAD_EXE_FORMAT)",
                        Descricao = "O arquivo executável não é um aplicativo Windows válido ou pertence a uma arquitetura incompatível (32/64 bits).",
                        AcaoSugerida = "Verifique se o binário não está corrompido e se a versão do executável corresponde ao ambiente do sistema operacional."
                    };

                case 740:
                    return new ErroDiagnostico
                    {
                        Codigo = 740,
                        Titulo = "Operação exige elevação (ERROR_ELEVATION_REQUIRED)",
                        Descricao = "O programa ou executável foi configurado para exigir privilégios de Administrador (UAC).",
                        AcaoSugerida = "Ajuste o executável ou atalho para executar sem privilégios de Administrador, pois o CIGAM roda no contexto do usuário padrão."
                    };

                case 1722:
                    return new ErroDiagnostico
                    {
                        Codigo = 1722,
                        Titulo = "Servidor de impressão RPC indisponível (RPC_S_SERVER_UNAVAILABLE)",
                        Descricao = "A comunicação com o Spooler de Impressão local ou com o servidor de impressão remoto falhou.",
                        AcaoSugerida = "Verifique se o serviço 'Spooler de Impressão' (Spooler) do Windows está em execução e se a conectividade de rede com o servidor de impressão está ativa."
                    };

                case 1801:
                    return new ErroDiagnostico
                    {
                        Codigo = 1801,
                        Titulo = "Nome de impressora inválido (ERROR_INVALID_PRINTER_NAME)",
                        Descricao = "O nome da impressora informado não foi reconhecido pelo subsistema de impressão.",
                        AcaoSugerida = "A impressora selecionada pode não existir nesta sessão. Em conexões Terminal Services/RDP, impressoras redirecionadas alteram seu sufixo numérico (ex.: 'HP LaserJet (redirecionado 2)') a cada reconexão; verifique se a impressora padrão da sessão atual está válida."
                    };

                default:
                    string msgWin32;
                    try
                    {
                        msgWin32 = new Win32Exception(codigoWin32).Message;
                    }
                    catch
                    {
                        msgWin32 = "Erro do sistema operacional Windows.";
                    }

                    return new ErroDiagnostico
                    {
                        Codigo = codigoWin32,
                        Titulo = $"Erro Win32 ({codigoWin32})",
                        Descricao = msgWin32,
                        AcaoSugerida = "Consulte o suporte técnico ou administrador do sistema informando o código e mensagem do erro."
                    };
            }
        }

        public static ErroDiagnostico ObterErro(Exception ex)
        {
            if (ex == null)
            {
                return new ErroDiagnostico
                {
                    Codigo = 0,
                    Titulo = "Erro desconhecido",
                    Descricao = "Nenhum detalhe de exceção disponível.",
                    AcaoSugerida = "Consulte o log do sistema."
                };
            }

            if (ex is Win32Exception win32Ex)
            {
                var diag = ObterErro(win32Ex.NativeErrorCode);
                if (!string.IsNullOrWhiteSpace(win32Ex.Message) && !diag.Descricao.Contains(win32Ex.Message))
                {
                    diag.Descricao += $" Detalhe: {win32Ex.Message}";
                }
                return diag;
            }

            int hresult = Marshal.GetHRForException(ex);
            int win32Code = hresult & 0xFFFF;

            if (win32Code > 0 && win32Code != 0xFFFF)
            {
                var diag = ObterErro(win32Code);
                diag.Descricao += $" Exceção: {ex.Message}";
                return diag;
            }

            return new ErroDiagnostico
            {
                Codigo = hresult,
                Titulo = ex.GetType().Name,
                Descricao = ex.Message,
                AcaoSugerida = "Verifique o log detalhado para obter informações adicionais sobre a falha."
            };
        }
    }
}
