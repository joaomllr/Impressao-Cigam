using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace CigamPrintTest.Config
{
    public class ConfigException : Exception
    {
        public string Campo { get; }
        public string CaminhoArquivo { get; }

        public ConfigException(string mensagem, string campo = null, string caminhoArquivo = null, Exception inner = null)
            : base(mensagem, inner)
        {
            Campo = campo ?? string.Empty;
            CaminhoArquivo = caminhoArquivo ?? string.Empty;
        }
    }

    public static class ConfigLoader
    {
        public const string NomeArquivoConfig = "CigamPrintTest.config.json";

        public static string ObterCaminhoPadrao()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NomeArquivoConfig);
        }

        public static string ObterJsonPadrao()
        {
            return @"{
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
    },
    {
      ""nome"": ""Teste do programa (Notepad)"",
      ""executavel"": ""C:\\Windows\\System32\\notepad.exe"",
      ""argumentos"": ""/p {arquivo}"",
      ""pastaExecucao"": """",
      ""esperarProcesso"": true,
      ""timeoutProcessoMs"": 60000,
      ""extensoesPermitidas"": ["".txt""],
      ""pdfEsperado"": """"
    }
  ]
}";
        }

        public static void CriarConfigPadrao(string caminho = null)
        {
            var destino = string.IsNullOrWhiteSpace(caminho) ? ObterCaminhoPadrao() : caminho;
            var pasta = Path.GetDirectoryName(destino);
            if (!string.IsNullOrEmpty(pasta) && !Directory.Exists(pasta))
            {
                Directory.CreateDirectory(pasta);
            }

            File.WriteAllText(destino, ObterJsonPadrao(), Encoding.UTF8);
        }

        public static ConfigApp Carregar(string caminho = null)
        {
            var arquivo = string.IsNullOrWhiteSpace(caminho) ? ObterCaminhoPadrao() : caminho;

            if (!File.Exists(arquivo))
            {
                throw new ConfigException(
                    $"Arquivo de configuração não encontrado: '{arquivo}'.",
                    campo: "arquivo",
                    caminhoArquivo: arquivo);
            }

            string conteudo;
            try
            {
                conteudo = File.ReadAllText(arquivo, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new ConfigException(
                    $"Falha ao ler o arquivo de configuração '{arquivo}': {ex.Message}",
                    campo: "arquivo",
                    caminhoArquivo: arquivo,
                    inner: ex);
            }

            return DesserializarEValidar(conteudo, arquivo);
        }

        public static ConfigApp DesserializarEValidar(string json, string caminhoOrigem = null)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ConfigException(
                    "O conteúdo do arquivo de configuração está vazio.",
                    campo: "arquivo",
                    caminhoArquivo: caminhoOrigem);
            }

            var serializer = new JavaScriptSerializer();
            object objRaiz;
            try
            {
                objRaiz = serializer.DeserializeObject(json);
            }
            catch (Exception ex)
            {
                throw new ConfigException(
                    $"Sintaxe JSON inválida no arquivo de configuração: {ex.Message}",
                    campo: "formato_json",
                    caminhoArquivo: caminhoOrigem,
                    inner: ex);
            }

            var dictRaiz = objRaiz as Dictionary<string, object>;
            if (dictRaiz == null)
            {
                throw new ConfigException(
                    "O JSON raiz de configuração deve ser um objeto {...}.",
                    campo: "formato_json",
                    caminhoArquivo: caminhoOrigem);
            }

            var config = new ConfigApp();

            // 1. pastaLogs
            if (!dictRaiz.ContainsKey("pastaLogs") || dictRaiz["pastaLogs"] == null || string.IsNullOrWhiteSpace(dictRaiz["pastaLogs"].ToString()))
            {
                throw new ConfigException(
                    "O campo 'pastaLogs' é obrigatório e não pode ser vazio.",
                    campo: "pastaLogs",
                    caminhoArquivo: caminhoOrigem);
            }
            config.PastaLogs = dictRaiz["pastaLogs"].ToString().Trim();

            // 2. timeoutDeteccaoJobMs
            config.TimeoutDeteccaoJobMs = LerInteiroPositivo(dictRaiz, "timeoutDeteccaoJobMs", caminhoOrigem, 60000);

            // 3. timeoutConclusaoJobMs
            config.TimeoutConclusaoJobMs = LerInteiroPositivo(dictRaiz, "timeoutConclusaoJobMs", caminhoOrigem, 120000);

            // 4. intervaloPollingMs
            config.IntervaloPollingMs = LerInteiroPositivo(dictRaiz, "intervaloPollingMs", caminhoOrigem, 500);

            // 5. perfilPadrao
            if (!dictRaiz.ContainsKey("perfilPadrao") || dictRaiz["perfilPadrao"] == null || string.IsNullOrWhiteSpace(dictRaiz["perfilPadrao"].ToString()))
            {
                throw new ConfigException(
                    "O campo 'perfilPadrao' é obrigatório e não pode ser vazio.",
                    campo: "perfilPadrao",
                    caminhoArquivo: caminhoOrigem);
            }
            config.PerfilPadrao = dictRaiz["perfilPadrao"].ToString().Trim();

            // 6. perfis
            var listaPerfis = dictRaiz.ContainsKey("perfis") ? (dictRaiz["perfis"] as IEnumerable)?.Cast<object>().ToList() : null;
            if (listaPerfis == null || listaPerfis.Count == 0)
            {
                throw new ConfigException(
                    "O campo 'perfis' deve ser uma lista não vazia de perfis de impressão.",
                    campo: "perfis",
                    caminhoArquivo: caminhoOrigem);
            }

            config.Perfis = new List<PerfilImpressao>();
            for (int i = 0; i < listaPerfis.Count; i++)
            {
                var dictPerfil = listaPerfis[i] as Dictionary<string, object>;
                if (dictPerfil == null)
                {
                    throw new ConfigException(
                        $"O perfil no índice {i} é inválido (deve ser um objeto).",
                        campo: $"perfis[{i}]",
                        caminhoArquivo: caminhoOrigem);
                }

                var perfil = new PerfilImpressao();

                if (!dictPerfil.ContainsKey("nome") || dictPerfil["nome"] == null || string.IsNullOrWhiteSpace(dictPerfil["nome"].ToString()))
                {
                    throw new ConfigException(
                        $"O campo 'nome' é obrigatório no perfil #{i + 1}.",
                        campo: $"perfis[{i}].nome",
                        caminhoArquivo: caminhoOrigem);
                }
                perfil.Nome = dictPerfil["nome"].ToString().Trim();

                if (!dictPerfil.ContainsKey("executavel") || dictPerfil["executavel"] == null || string.IsNullOrWhiteSpace(dictPerfil["executavel"].ToString()))
                {
                    throw new ConfigException(
                        $"O campo 'executavel' é obrigatório no perfil '{perfil.Nome}'.",
                        campo: $"perfis[{i}].executavel",
                        caminhoArquivo: caminhoOrigem);
                }
                perfil.Executavel = dictPerfil["executavel"].ToString().Trim();

                perfil.Argumentos = dictPerfil.ContainsKey("argumentos") && dictPerfil["argumentos"] != null ? dictPerfil["argumentos"].ToString() : string.Empty;
                perfil.PastaExecucao = dictPerfil.ContainsKey("pastaExecucao") && dictPerfil["pastaExecucao"] != null ? dictPerfil["pastaExecucao"].ToString() : string.Empty;

                if (dictPerfil.ContainsKey("esperarProcesso") && dictPerfil["esperarProcesso"] is bool esp)
                {
                    perfil.EsperarProcesso = esp;
                }
                else if (dictPerfil.ContainsKey("esperarProcesso") && bool.TryParse(dictPerfil["esperarProcesso"]?.ToString(), out var bVal))
                {
                    perfil.EsperarProcesso = bVal;
                }

                if (dictPerfil.ContainsKey("timeoutProcessoMs"))
                {
                    if (int.TryParse(dictPerfil["timeoutProcessoMs"]?.ToString(), out var tMs) && tMs > 0)
                    {
                        perfil.TimeoutProcessoMs = tMs;
                    }
                    else
                    {
                        throw new ConfigException(
                            $"O campo 'timeoutProcessoMs' no perfil '{perfil.Nome}' deve ser um número inteiro maior que zero.",
                            campo: $"perfis[{i}].timeoutProcessoMs",
                            caminhoArquivo: caminhoOrigem);
                    }
                }

                if (dictPerfil.ContainsKey("extensoesPermitidas") && dictPerfil["extensoesPermitidas"] is IEnumerable exts)
                {
                    perfil.ExtensoesPermitidas = exts
                        .OfType<object>()
                        .Select(e => e?.ToString()?.Trim())
                        .Where(e => !string.IsNullOrEmpty(e))
                        .Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant())
                        .ToList();
                }
                else
                {
                    perfil.ExtensoesPermitidas = new List<string>();
                }

                perfil.PdfEsperado = dictPerfil.ContainsKey("pdfEsperado") && dictPerfil["pdfEsperado"] != null ? dictPerfil["pdfEsperado"].ToString() : string.Empty;

                config.Perfis.Add(perfil);
            }

            // Validar que perfilPadrao existe na lista de perfis
            if (!config.Perfis.Any(p => string.Equals(p.Nome, config.PerfilPadrao, StringComparison.OrdinalIgnoreCase)))
            {
                var nomesDisponiveis = string.Join(", ", config.Perfis.Select(p => $"'{p.Nome}'"));
                throw new ConfigException(
                    $"O perfil padrão '{config.PerfilPadrao}' não foi encontrado na lista de perfis configurados. Perfis disponíveis: {nomesDisponiveis}.",
                    campo: "perfilPadrao",
                    caminhoArquivo: caminhoOrigem);
            }

            return config;
        }

        private static int LerInteiroPositivo(Dictionary<string, object> dict, string campo, string caminhoOrigem, int valorPadrao)
        {
            if (!dict.ContainsKey(campo) || dict[campo] == null)
            {
                throw new ConfigException(
                    $"O campo '{campo}' é obrigatório.",
                    campo: campo,
                    caminhoArquivo: caminhoOrigem);
            }

            if (!int.TryParse(dict[campo].ToString(), out var valor) || valor <= 0)
            {
                throw new ConfigException(
                    $"O campo '{campo}' deve ser um número inteiro maior que zero (valor informado: '{dict[campo]}').",
                    campo: campo,
                    caminhoArquivo: caminhoOrigem);
            }

            return valor;
        }
    }
}
