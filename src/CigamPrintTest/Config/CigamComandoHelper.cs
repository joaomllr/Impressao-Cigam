using System;
using System.IO;
using System.Text.RegularExpressions;

namespace CigamPrintTest.Config
{
    /// <summary>
    /// Implementa a regra do CIGAM para montagem de linha de comando baseada na config LF-NE-2128
    /// e detecção automática da pasta de instalação (%CIGAM_INSTAL%).
    /// </summary>
    public static class CigamComandoHelper
    {
        public const string ExemploConfig2128Padrao = @"%CIGAM_INSTAL%CGEditor.exe -VG: -A:";
        public const string ArquivoExemploPreview = @"C:\exemplo\DANFE30000044312.rtf";

        /// <summary>
        /// Normaliza a pasta de instalação do CIGAM para sempre terminar em '\'.
        /// </summary>
        public static string NormalizarCigamInstal(string instal)
        {
            if (string.IsNullOrWhiteSpace(instal))
            {
                return string.Empty;
            }

            var s = instal.Trim().Replace('/', '\\');
            if (!s.EndsWith("\\"))
            {
                s += "\\";
            }
            return s;
        }

        /// <summary>
        /// Monta a linha de comando completa segundo a regra do programa Imprime DANFE (exp #38):
        /// comando = Trim(valorConfig2128) & Trim(caminhoRtf) & IF(valorConfig2128 contém '"', '"', '')
        /// onde %CIGAM_INSTAL% dentro do valor é substituído pela pasta de instalação do CIGAM do cliente.
        /// </summary>
        public static string MontarLinhaComando(string valorConfig2128, string cigamInstal, string caminhoRtf)
        {
            var valorOriginal = valorConfig2128 ?? string.Empty;
            var instalNormalizada = NormalizarCigamInstal(cigamInstal);

            // Substitui %CIGAM_INSTAL% (case-insensitive) pelo caminho normalizado
            var valorSubstituido = Regex.Replace(
                valorOriginal,
                Regex.Escape("%CIGAM_INSTAL%"),
                m => instalNormalizada,
                RegexOptions.IgnoreCase);

            // Caminho RTF sem aspas extras nas bordas para montagem limpa
            var rtfLimpo = (caminhoRtf ?? string.Empty).Trim().Trim('\"');

            // IF(valorConfig2128 contém '"', '"', '')
            bool contemAspas = valorOriginal.Contains("\"");
            string aspaFinal = contemAspas ? "\"" : string.Empty;

            return valorSubstituido.Trim() + rtfLimpo + aspaFinal;
        }

        /// <summary>
        /// Separa executável e argumentos conforme regra do CIGAM:
        /// - se a linha começa com aspas, o executável vai até a próxima aspa;
        /// - senão, até o primeiro espaço;
        /// - o resto vira os argumentos, exatamente como montado (sem adicionar nem remover aspas).
        /// </summary>
        public static void SepararExecutavelEArgumentos(string linhaComando, out string executavel, out string argumentos)
        {
            if (string.IsNullOrWhiteSpace(linhaComando))
            {
                executavel = string.Empty;
                argumentos = string.Empty;
                return;
            }

            var linha = linhaComando.Trim();

            if (linha.StartsWith("\""))
            {
                int proximaAspa = linha.IndexOf('\"', 1);
                if (proximaAspa > 0)
                {
                    executavel = linha.Substring(1, proximaAspa - 1);
                    var resto = linha.Substring(proximaAspa + 1);
                    argumentos = resto.TrimStart(' ');
                }
                else
                {
                    executavel = linha.Trim('\"');
                    argumentos = string.Empty;
                }
            }
            else
            {
                int primeiroEspaco = linha.IndexOf(' ');
                if (primeiroEspaco > 0)
                {
                    executavel = linha.Substring(0, primeiroEspaco);
                    var resto = linha.Substring(primeiroEspaco + 1);
                    argumentos = resto.TrimStart(' ');
                }
                else
                {
                    executavel = linha;
                    argumentos = string.Empty;
                }
            }
        }

        /// <summary>
        /// Resolve linha de comando completa, executável e argumentos a partir dos dados da config.
        /// </summary>
        public static void Resolver(
            string valorConfig2128,
            string cigamInstal,
            string caminhoRtf,
            out string executavel,
            out string argumentos,
            out string linhaCompleta)
        {
            linhaCompleta = MontarLinhaComando(valorConfig2128, cigamInstal, caminhoRtf);
            SepararExecutavelEArgumentos(linhaCompleta, out executavel, out argumentos);
        }

        /// <summary>
        /// Procura a pasta de instalação do CIGAM subindo até 'niveis' diretórios
        /// a partir da pasta base (padrão: diretório do executável atual) procurando CGEditor.exe.
        /// Retorna a pasta terminada em '\' se encontrar, ou string vazia se não encontrar.
        /// </summary>
        public static string DetectarPastaCigam(string pastaBase = null, int niveis = 5)
        {
            try
            {
                var inicio = string.IsNullOrWhiteSpace(pastaBase)
                    ? AppDomain.CurrentDomain.BaseDirectory
                    : pastaBase;

                if (string.IsNullOrWhiteSpace(inicio) || !Directory.Exists(inicio))
                {
                    return string.Empty;
                }

                var dirAtual = new DirectoryInfo(inicio);

                for (int i = 0; i <= niveis && dirAtual != null; i++)
                {
                    // 1. Verifica se CGEditor.exe está diretamente nesta pasta
                    var arqCgEditor = Path.Combine(dirAtual.FullName, "CGEditor.exe");
                    if (File.Exists(arqCgEditor))
                    {
                        return NormalizarCigamInstal(dirAtual.FullName);
                    }

                    // 2. Verifica subpastas imediatas comuns (ex: CIGAM11, bin)
                    try
                    {
                        var subpastas = dirAtual.GetDirectories();
                        foreach (var sub in subpastas)
                        {
                            var arqSub = Path.Combine(sub.FullName, "CGEditor.exe");
                            if (File.Exists(arqSub))
                            {
                                return NormalizarCigamInstal(sub.FullName);
                            }
                        }
                    }
                    catch
                    {
                        // Ignora erro de permissão em subpastas
                    }

                    dirAtual = dirAtual.Parent;
                }
            }
            catch
            {
                // Ignora falhas de I/O na busca
            }

            return string.Empty;
        }
    }
}
