using System;
using System.IO;
using System.Linq;
using CigamPrintTest.Config;
using CigamPrintTest.Erros;

namespace CigamPrintTest.Diagnostico
{
    public class ArquivoInfo
    {
        public string CaminhoOriginal { get; set; } = string.Empty;
        public string CaminhoResolvido { get; set; } = string.Empty;
        public bool Existe { get; set; }
        public long TamanhoBytes { get; set; }
        public bool PodeSerLido { get; set; }
        public string Extensao { get; set; } = string.Empty;
        public bool ExtensaoPermitida { get; set; }
        public bool TemEspacosNoCaminho { get; set; }
        public bool AlertaEspacosSemAspas { get; set; }
        public bool Sucesso { get; set; }
        public bool TemAviso { get; set; }
        public string MensagemStatus { get; set; } = string.Empty;
        public ErroDiagnostico Erro { get; set; }

        public static ArquivoInfo Analisar(string caminhoArquivo, PerfilImpressao perfil)
        {
            var info = new ArquivoInfo
            {
                CaminhoOriginal = caminhoArquivo ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(caminhoArquivo))
            {
                info.Sucesso = false;
                info.Erro = CatalogoErros.ObterErro(2);
                info.MensagemStatus = "Nenhum arquivo foi selecionado ou informado para o teste.";
                return info;
            }

            string caminhoLimpo = caminhoArquivo.Trim('\"');
            info.CaminhoResolvido = Environment.ExpandEnvironmentVariables(caminhoLimpo);

            if (!File.Exists(info.CaminhoResolvido))
            {
                info.Existe = false;
                info.Sucesso = false;
                info.Erro = CatalogoErros.ObterErro(2);
                info.MensagemStatus = $"Arquivo não encontrado: '{info.CaminhoResolvido}'. {info.Erro}";
                return info;
            }

            info.Existe = true;

            try
            {
                var fi = new FileInfo(info.CaminhoResolvido);
                info.TamanhoBytes = fi.Length;
                info.Extensao = fi.Extension.ToLowerInvariant();
            }
            catch (Exception ex)
            {
                info.Sucesso = false;
                info.Erro = CatalogoErros.ObterErro(ex);
                info.MensagemStatus = $"Falha ao inspecionar propriedades do arquivo: {info.Erro}";
                return info;
            }

            if (info.TamanhoBytes <= 0)
            {
                info.Sucesso = false;
                info.MensagemStatus = $"O arquivo '{info.CaminhoResolvido}' está vazio (0 bytes). Documentos de impressão válidos devem ter conteúdo.";
                return info;
            }

            // Testar se pode ser aberto para leitura compartilhada
            try
            {
                using (var fs = File.Open(info.CaminhoResolvido, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    info.PodeSerLido = true;
                }
            }
            catch (Exception ex)
            {
                info.PodeSerLido = false;
                info.Sucesso = false;
                info.Erro = CatalogoErros.ObterErro(ex);
                info.MensagemStatus = $"Arquivo bloqueado para leitura: {info.Erro}";
                return info;
            }

            // Validar extensão se o perfil tiver restrições
            if (perfil != null && perfil.ExtensoesPermitidas != null && perfil.ExtensoesPermitidas.Count > 0)
            {
                var permitidas = perfil.ExtensoesPermitidas
                    .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
                    .ToList();

                info.ExtensaoPermitida = permitidas.Contains(info.Extensao);
                if (!info.ExtensaoPermitida)
                {
                    info.Sucesso = false;
                    info.MensagemStatus = $"Extensão '{info.Extensao}' não é permitida para o perfil '{perfil.Nome}'. Extensões aceitas: {string.Join(", ", permitidas)}.";
                    return info;
                }
            }
            else
            {
                info.ExtensaoPermitida = true;
            }

            // Verificar se caminho contém espaços e o perfil usa {arquivoSemAspas}
            info.TemEspacosNoCaminho = info.CaminhoResolvido.Contains(" ");
            bool passaSemAspas = perfil != null && (
                (perfil.Argumentos != null && perfil.Argumentos.Contains("{arquivoSemAspas}")) ||
                (!string.IsNullOrWhiteSpace(perfil.ComandoCigam) && !perfil.ComandoCigam.Contains("\"")));
            if (info.TemEspacosNoCaminho && passaSemAspas)
            {
                info.AlertaEspacosSemAspas = true;
                info.TemAviso = true;
                info.Sucesso = true;
                info.MensagemStatus = $"AVISO: O caminho contém espaços ('{info.CaminhoResolvido}') e o perfil utiliza {{arquivoSemAspas}}. O CIGAM passa este caminho sem aspas; se falhar aqui, falharia no CIGAM também. Tamanho: {info.TamanhoBytes} bytes.";
                return info;
            }

            info.Sucesso = true;
            info.MensagemStatus = $"Arquivo validado com sucesso: '{info.CaminhoResolvido}', {info.TamanhoBytes:N0} bytes, extensão '{info.Extensao}' permitida.";
            return info;
        }
    }
}
