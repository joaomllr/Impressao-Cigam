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
  ""configurado"": false,
  ""cliente"": """",
  ""cigamInstal"": """",
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
                    $"Arquivo de configuração não encontrado em: '{arquivo}'.",
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
                    $"Não foi possível ler o arquivo de configuração: {ex.Message}",
                    campo: "arquivo",
                    caminhoArquivo: arquivo,
                    inner: ex);
            }

            return DesserializarEValidar(conteudo, arquivo);
        }

        public static ConfigApp DesserializarEValidar(string json, string caminhoOrigem = "em_memoria.json")
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ConfigException(
                    "O conteúdo do arquivo de configuração está vazio.",
                    campo: "conteudo",
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

            // Campos novos opcionais (retrocompatíveis)
            if (dictRaiz.ContainsKey("configurado") && dictRaiz["configurado"] != null)
            {
                if (dictRaiz["configurado"] is bool bConf)
                {
                    config.Configurado = bConf;
                }
                else if (bool.TryParse(dictRaiz["configurado"].ToString(), out var bParsed))
                {
                    config.Configurado = bParsed;
                }
            }

            if (dictRaiz.ContainsKey("cliente") && dictRaiz["cliente"] != null)
            {
                config.Cliente = dictRaiz["cliente"].ToString().Trim();
            }

            if (dictRaiz.ContainsKey("cigamInstal") && dictRaiz["cigamInstal"] != null)
            {
                config.CigamInstal = CigamComandoHelper.NormalizarCigamInstal(dictRaiz["cigamInstal"].ToString());
            }

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

                if (dictPerfil.ContainsKey("comandoCigam") && dictPerfil["comandoCigam"] != null)
                {
                    perfil.ComandoCigam = dictPerfil["comandoCigam"].ToString().Trim();
                }

                if (dictPerfil.ContainsKey("cigamInstal") && dictPerfil["cigamInstal"] != null && !string.IsNullOrWhiteSpace(dictPerfil["cigamInstal"].ToString()))
                {
                    perfil.CigamInstal = CigamComandoHelper.NormalizarCigamInstal(dictPerfil["cigamInstal"].ToString());
                }
                else
                {
                    perfil.CigamInstal = config.CigamInstal;
                }

                if (dictPerfil.ContainsKey("executavel") && dictPerfil["executavel"] != null && !string.IsNullOrWhiteSpace(dictPerfil["executavel"].ToString()))
                {
                    perfil.Executavel = dictPerfil["executavel"].ToString().Trim();
                }
                else if (!string.IsNullOrWhiteSpace(perfil.ComandoCigam))
                {
                    // Se comandoCigam está preenchido, deriva executável dele
                    var linha = CigamComandoHelper.MontarLinhaComando(perfil.ComandoCigam, perfil.CigamInstal, string.Empty);
                    CigamComandoHelper.SepararExecutavelEArgumentos(linha, out var exeDerivado, out _);
                    perfil.Executavel = exeDerivado;
                }
                else
                {
                    throw new ConfigException(
                        $"O campo 'executavel' é obrigatório no perfil '{perfil.Nome}'.",
                        campo: $"perfis[{i}].executavel",
                        caminhoArquivo: caminhoOrigem);
                }

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

        /// <summary>
        /// Serializa a configuração para formato JSON formatado com recuo.
        /// </summary>
        public static string Serializar(ConfigApp config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"configurado\": {(config.Configurado ? "true" : "false")},");
            sb.AppendLine($"  \"cliente\": \"{EscapeJson(config.Cliente ?? string.Empty)}\",");
            sb.AppendLine($"  \"cigamInstal\": \"{EscapeJson(config.CigamInstal ?? string.Empty)}\",");
            sb.AppendLine($"  \"pastaLogs\": \"{EscapeJson(config.PastaLogs ?? string.Empty)}\",");
            sb.AppendLine($"  \"timeoutDeteccaoJobMs\": {config.TimeoutDeteccaoJobMs},");
            sb.AppendLine($"  \"timeoutConclusaoJobMs\": {config.TimeoutConclusaoJobMs},");
            sb.AppendLine($"  \"intervaloPollingMs\": {config.IntervaloPollingMs},");
            sb.AppendLine($"  \"perfilPadrao\": \"{EscapeJson(config.PerfilPadrao ?? string.Empty)}\",");
            sb.AppendLine("  \"perfis\": [");

            for (int i = 0; i < config.Perfis.Count; i++)
            {
                var p = config.Perfis[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"nome\": \"{EscapeJson(p.Nome)}\",");

                if (!string.IsNullOrWhiteSpace(p.ComandoCigam))
                {
                    sb.AppendLine($"      \"comandoCigam\": \"{EscapeJson(p.ComandoCigam)}\",");
                }

                if (!string.IsNullOrWhiteSpace(p.Executavel))
                {
                    sb.AppendLine($"      \"executavel\": \"{EscapeJson(p.Executavel)}\",");
                }

                if (!string.IsNullOrWhiteSpace(p.Argumentos) || string.IsNullOrWhiteSpace(p.ComandoCigam))
                {
                    sb.AppendLine($"      \"argumentos\": \"{EscapeJson(p.Argumentos)}\",");
                }

                sb.AppendLine($"      \"pastaExecucao\": \"{EscapeJson(p.PastaExecucao)}\",");
                sb.AppendLine($"      \"esperarProcesso\": {(p.EsperarProcesso ? "true" : "false")},");
                sb.AppendLine($"      \"timeoutProcessoMs\": {p.TimeoutProcessoMs},");

                sb.AppendLine("      \"extensoesPermitidas\": [");
                if (p.ExtensoesPermitidas != null && p.ExtensoesPermitidas.Count > 0)
                {
                    for (int j = 0; j < p.ExtensoesPermitidas.Count; j++)
                    {
                        var ext = p.ExtensoesPermitidas[j];
                        var virgula = j < p.ExtensoesPermitidas.Count - 1 ? "," : "";
                        sb.AppendLine($"        \"{EscapeJson(ext)}\"{virgula}");
                    }
                }
                sb.AppendLine("      ],");

                sb.AppendLine($"      \"pdfEsperado\": \"{EscapeJson(p.PdfEsperado)}\"");

                var virgulaPerfil = i < config.Perfis.Count - 1 ? "," : "";
                sb.AppendLine($"    }}{virgulaPerfil}");
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\r", "\\r")
                    .Replace("\n", "\\n")
                    .Replace("\t", "\\t");
        }

        /// <summary>
        /// Salva a configuração no arquivo especificado (ou no caminho padrão).
        /// </summary>
        public static void Salvar(ConfigApp config, string caminho = null)
        {
            var destino = string.IsNullOrWhiteSpace(caminho) ? ObterCaminhoPadrao() : caminho;
            var pasta = Path.GetDirectoryName(destino);
            if (!string.IsNullOrEmpty(pasta) && !Directory.Exists(pasta))
            {
                Directory.CreateDirectory(pasta);
            }

            var json = Serializar(config);
            File.WriteAllText(destino, json, Encoding.UTF8);
        }

        /// <summary>
        /// Avalia se o assistente de configuração inicial deve ser exibido.
        /// Retorna true se:
        /// - config ausente no disco;
        /// - config == null ou 'configurado' = false/ausente;
        /// - executável do perfil padrão inexistente no disco.
        /// </summary>
        public static bool DeveExibirAssistente(ConfigApp config, string caminhoConfig = null)
        {
            if (!string.IsNullOrWhiteSpace(caminhoConfig) && !File.Exists(caminhoConfig))
            {
                return true;
            }

            if (config == null)
            {
                return true;
            }

            if (!config.Configurado)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(config.PerfilPadrao) || config.Perfis == null || config.Perfis.Count == 0)
            {
                return true;
            }

            var perfilPadrao = config.Perfis.FirstOrDefault(p =>
                string.Equals(p.Nome, config.PerfilPadrao, StringComparison.OrdinalIgnoreCase));

            if (perfilPadrao == null)
            {
                return true;
            }

            // Resolve executável (com suporte a comandoCigam ou executavel clássico)
            string executavel = string.Empty;
            try
            {
                if (!string.IsNullOrWhiteSpace(perfilPadrao.ComandoCigam))
                {
                    var linha = CigamComandoHelper.MontarLinhaComando(perfilPadrao.ComandoCigam, perfilPadrao.CigamInstal, string.Empty);
                    CigamComandoHelper.SepararExecutavelEArgumentos(linha, out executavel, out _);
                }
                else
                {
                    executavel = Environment.ExpandEnvironmentVariables(perfilPadrao.Executavel ?? string.Empty).Trim('\"');
                }
            }
            catch
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(executavel) || !File.Exists(executavel))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sobrecarga que verifica o arquivo de configuração no caminho padrão ou especificado.
        /// </summary>
        public static bool DeveExibirAssistente(string caminhoConfig = null)
        {
            var caminho = string.IsNullOrWhiteSpace(caminhoConfig) ? ObterCaminhoPadrao() : caminhoConfig;
            if (!File.Exists(caminho))
            {
                return true;
            }

            try
            {
                var config = Carregar(caminho);
                return DeveExibirAssistente(config, caminho);
            }
            catch
            {
                return true;
            }
        }
    }
}
