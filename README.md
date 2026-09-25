# Diagnóstico de Impressão ERP CIGAM (`CigamPrintTest`)

Ferramenta especializada em diagnóstico e homologação do fluxo de impressão de DANFE e documentos fiscais do ERP CIGAM no ambiente Windows (Sessão Local/Console e Terminal Services/RDP).

---

## 1. Contexto e Origem do Comando CIGAM

No ERP CIGAM, a impressão do DANFE é orquestrada pelo módulo fiscal através do seguinte fluxo confirmado no código-fonte e logs operacionais:

- **Configuração do CIGAM:** Parâmetro `LF-NE-2128` (*"Comando para Visualização/Edição da DANFE"*).
- **Programa:** *Imprime DANFE*.
- **Nome Público:** `NE00034`.
- **Componente:** `CGNFe`.
- **Mecanismo de Execução:** Chamada de sistema operacional (*Invoke OS Cmd*), sem aguardar o encerramento do processo (`esperarProcesso = false`).
- **Comando Padrão Disparado:**
  ```cmd
  C:\Cigam\Laminort\CIGAM11\CGEditor.exe -VG: -A:<caminho do RTF>
  ```
- **Particularidade Crítica de Formatação:** O CIGAM concatena o caminho do arquivo RTF **SEM aspas** logo após o parâmetro `-A:`. O comando é disparado uma vez por via solicitada.
- **Assincronismo:** O executável `CGEditor.exe` converte o RTF e envia os dados diretamente ao Spooler do Windows. O ERP CIGAM não recebe nenhum código de retorno nem confirmação da impressão. Portanto, a **fila de impressão do Windows (Spooler)** é a única fonte da verdade.

> **Importante:** Se o parâmetro `LF-NE-2128` for alterado nas configurações do CIGAM, o perfil correspondente no arquivo `CigamPrintTest.config.json` deve ser atualizado para manter a fidelidade ao ambiente de produção.

---

## 2. As 9 Etapas de Diagnóstico

O teste executa até 9 etapas sequenciais e **interrompe na primeira falha detectada**, permitindo isolar com precisão a camada causadora do problema:

| Etapa | Nome | O que verifica e diagnostica |
| :---: | :--- | :--- |
| **1** | **AMBIENTE** | Identifica usuário logado, estação de trabalho, tipo de sessão Windows (`Console` ou `RDP-Tcp#N`), `CLIENTNAME` da máquina remota, arquitetura do processo (32/64 bits) e valida se os diretórios temporários (`%TEMP%`) e de log (`%LOCALAPPDATA%\CigamPrintTest\logs`) possuem permissão real de escrita. |
| **2** | **ARQUIVO** | Garante que o arquivo de entrada existe, possui tamanho maior que 0 bytes, não está bloqueado com trava exclusiva por outro aplicativo (ex.: Word, CIGAM ou antivírus) e possui extensão autorizada pelo perfil. Emite **AVISO** caso o caminho contenha espaços e o perfil utilize `{arquivoSemAspas}`, pois o CIGAM falhará no mesmo cenário. |
| **3** | **VISUALIZADOR** | Confirma a existência física do executável configurado (ex.: `CGEditor.exe` ou `notepad.exe`) e monta a linha de comando exata com expansão de variáveis de ambiente. |
| **4** | **SNAPSHOT DAS FILAS** | Captura o estado e os IDs de todos os jobs preexistentes em todas as filas visíveis na sessão (locais, de rede e redirecionadas por Terminal Services/RDP), gravando o horário base `T0`. |
| **5** | **EXECUÇÃO** | Dispara o executável via `Process.Start` com `UseShellExecute = false` e diretório de trabalho adequado, repetindo o disparo para cada via solicitada e capturando o PID. Se configurado para esperar, monitora o código de saída (*exit code*). |
| **6** | **PDF (INTERMEDIÁRIO)** | Se o perfil definir `pdfEsperado`, monitora a geração do arquivo PDF intermediário até que seu tamanho se estabilize por duas leituras consecutivas. |
| **7** | **DETECÇÃO DO(S) JOB(S)** | Realiza polling no Spooler do Windows até `timeoutDeteccaoJobMs`, procurando novos jobs submetidos pelo usuário atual após `T0` que não existiam no snapshot inicial. Deve detectar 1 job por via. |
| **8** | **CONCLUSÃO (SPOOLER)** | Acompanha os jobs detectados até `timeoutConclusaoJobMs`. Valida se o job sumiu da fila sem flags de erro ou finalizou com status `Printed`/`Completed`. Lê as flags da fila (`IsOffline`, `IsInError`, `IsOutOfPaper`, `IsPaperJammed`, `IsPaused`, etc.). |
| **9** | **CONFIRMAÇÃO VISUAL** | Questiona o operador se o documento realmente saiu fisicamente na impressora, complementando o diagnóstico de portas TCP/IP sem SNMP ou impressoras redirecionadas de RDP. |

---

## 3. Matriz de Diagnóstico Final (Causa Raiz)

Ao término do teste, o sistema emite uma frase conclusiva identificando a camada exata da falha:

- **Falha na Etapa 1:** *Camada 1 (Ambiente/Permissão)*: Falha nas permissões do usuário, diretório temporário ou sessão Windows.
- **Falha na Etapa 2:** *Camada 2 (Arquivo)*: Arquivo inexistente, vazio, bloqueado por outro aplicativo ou extensão incompatível.
- **Falha na Etapa 3:** *Camada 3 (Visualizador)*: `CGEditor.exe` ausente na estação, caminho incorreto ou perfil errado.
- **Falha na Etapa 5:** *Camada 5 (Execução)*: O CGEditor não abriu ou retornou código de erro no encerramento.
- **Falha na Etapa 6:** *Camada 6 (Conversor/PDF)*: O visualizador/conversor não gerou o arquivo intermediário (falha fora da impressora).
- **Falha na Etapa 7:** *Camada 7 (Seleção de Impressora/Driver)*: O CGEditor abriu mas não enviou nada para a fila (impressora incorreta, diálogo cancelado ou driver recusou).
- **Falha na Etapa 8:** *Camada 8 (Fila/Driver/Porta)*: Falha no Spooler, driver incompatível, impressora offline ou porta de rede.
- **Etapas 1-8 OK e resposta "Não" na Etapa 9:** *Problema físico ou porta sem retorno de status*: Documento entregue ao Spooler, mas não saiu na impressora (verifique falta de papel, toner, cabo ou fila travada no hardware).

---

## 4. Configuração (`CigamPrintTest.config.json`)

O arquivo deve permanecer ao lado do executável (`CigamPrintTest.exe`):

```json
{
  "pastaLogs": "%LOCALAPPDATA%\\CigamPrintTest\\logs",
  "timeoutDeteccaoJobMs": 60000,
  "timeoutConclusaoJobMs": 120000,
  "intervaloPollingMs": 500,
  "perfilPadrao": "CIGAM - DANFE (CGEditor)",
  "perfis": [
    {
      "nome": "CIGAM - DANFE (CGEditor)",
      "executavel": "C:\\Cigam\\Laminort\\CIGAM11\\CGEditor.exe",
      "argumentos": "-VG: -A:{arquivoSemAspas}",
      "pastaExecucao": "",
      "esperarProcesso": false,
      "timeoutProcessoMs": 60000,
      "extensoesPermitidas": [".rtf"],
      "pdfEsperado": ""
    },
    {
      "nome": "Teste do programa (Notepad)",
      "executavel": "C:\\Windows\\System32\\notepad.exe",
      "argumentos": "/p {arquivo}",
      "pastaExecucao": "",
      "esperarProcesso": true,
      "timeoutProcessoMs": 60000,
      "extensoesPermitidas": [".txt"],
      "pdfEsperado": ""
    }
  ]
}
```

### Placeholders Suportados
- `{arquivo}`: Caminho completo envolvido por aspas (`"C:\pasta\arquivo.rtf"`).
- `{arquivoSemAspas}`: Caminho exato sem aspas (padrão CIGAM `-A:C:\pasta\arquivo.rtf`).
- `{nomeArquivo}`: Nome do arquivo com extensão (`DANFE123.rtf`).
- `{nomeSemExtensao}`: Nome do arquivo sem extensão (`DANFE123`).
- `{pasta}`: Diretório do arquivo (`C:\pasta`).
- `{codTeste}`: Código do teste (`TyyyyMMddHHmmss`).
- `{usuario}`: Usuário logado no Windows.
- `{maquina}`: Nome da estação de trabalho.
- `{via}`: Número da via atual.
- Variáveis de ambiente como `%TEMP%`, `%LOCALAPPDATA%` e `%USERNAME%` são automaticamente expandidas.

---

## 5. Parâmetros de Linha de Comando

```cmd
CigamPrintTest.exe [--arquivo "caminho"] [--perfil "nome"] [--vias N] [--auto]
```

- **Sem argumentos:** Abre a interface gráfica com o perfil padrão selecionado.
- `--arquivo "caminho"`: Define o arquivo a ser testado.
- `--perfil "nome"`: Seleciona o perfil de impressão pelo nome.
- `--vias N`: Quantidade de vias (1 a 10).
- `--auto`: Executa o teste imediatamente ao abrir e mantém a tela aberta com os resultados detalhados e diagnóstico pronto para cópia.
