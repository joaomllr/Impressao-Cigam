# Guia de Compilação e Deploy (`CigamPrintTest`)

Instruções para compilação, distribuição, configuração de menu no ERP CIGAM e checklist de homologação em estações locais e sessões de Terminal Services (RDP).

---

## 1. Compilação (Build)

### Pré-requisitos
- Sistema Operacional: Windows 10/11 ou Windows Server 2016/2019/2022.
- .NET Framework 4.8 Runtime instalado.
- .NET SDK (8.0 ou superior) para compilação SDK-style.

### Comando de Compilação
Na raiz do projeto ou executando o script `build.bat`:

```cmd
dotnet build -c Release
```

Para executar a suíte completa de testes unitários:
```cmd
dotnet test -c Release
```

### Arquivos Gerados para Publicação
Os artefatos de saída estarão localizados em:
`src\CigamPrintTest\bin\Release\net48\`

Arquivos estritamente necessários para o deploy:
1. `CigamPrintTest.exe` (Executável AnyCPU WinForms)
2. `CigamPrintTest.config.json` (Arquivo de configuração de perfis e timeouts)
3. `CigamPrintTest.exe.config` (Arquivo de configuração do runtime .NET)

> **Restrição Inegociável:** Sem dependências externas de pacotes NuGet em runtime, sem serviços do Windows, sem instalação e sem exigência de privilégios de Administrador. O deploy consiste unicamente em copiar a pasta.

---

## 2. Instalação e Estrutura de Pastas Sugerida

Copie os arquivos para o diretório de ferramentas do CIGAM na estação ou servidor de aplicação:

```cmd
C:\Cigam\Laminort\CIGAM11\Ferramentas\CigamPrintTest\
    ├── CigamPrintTest.exe
    ├── CigamPrintTest.config.json
    └── CigamPrintTest.exe.config
```

---

## 3. Apontamento no Menu do CIGAM

O executável foi projetado para ser chamado diretamente a partir do menu do CIGAM pelo operador ou equipe de suporte técnico na mesma sessão em que o ERP está em execução.

### [DEFINIR CONFORME CADASTRO DE MENU DO CIGAM]
No cadastro de menus / programas externos do CIGAM:

- **Tipo de Execução:** Executável Externo / Invoke OS Cmd
- **Programa:** `C:\Cigam\Laminort\CIGAM11\Ferramentas\CigamPrintTest\CigamPrintTest.exe`
- **Diretório de Trabalho:** `C:\Cigam\Laminort\CIGAM11\Ferramentas\CigamPrintTest\`

### Exemplos de Argumentos de Linha de Comando

1. **Abertura Interativa (Padrão para Operador/Suporte):**
   ```cmd
   C:\Cigam\Laminort\CIGAM11\Ferramentas\CigamPrintTest\CigamPrintTest.exe
   ```
   *Abre a tela permitindo escolher o perfil, selecionar o arquivo RTF e disparar os testes.*

2. **Atalho com Arquivo de DANFE Recente Pré-Carregado:**
   ```cmd
   C:\Cigam\Laminort\CIGAM11\Ferramentas\CigamPrintTest\CigamPrintTest.exe --arquivo "%TEMP%\DANFE_ULTIMA.rtf"
   ```

3. **Diagnóstico Automático (Modo Headless / Validação Direta):**
   ```cmd
   C:\Cigam\Laminort\CIGAM11\Ferramentas\CigamPrintTest\CigamPrintTest.exe --perfil "CIGAM - DANFE (CGEditor)" --arquivo "%TEMP%\DANFE_ULTIMA.rtf" --vias 1 --auto
   ```
   *Executa as 9 etapas automaticamente e mantém a tela aberta com o diagnóstico pronto para cópia.*

---

## 4. Checklist de Homologação

### Checklist A: Estação Local (Console)
- [ ] Executável abre normalmente sem solicitar elevação de UAC (Administrador).
- [ ] Banner superior exibe `Sessão: Local/Console (Console)` e o usuário correto.
- [ ] Etapa 1 valida que `%TEMP%` e a pasta de logs diários possuem permissão de gravação.
- [ ] Grade de impressoras lista todas as impressoras locais instaladas e indica a padrão.
- [ ] Botão "Diagnóstico do ambiente" valida etapas 1 e 2 sem disparar impressão.
- [ ] Teste com perfil "Teste do programa (Notepad)" utilizando um `.txt` e a impressora "Microsoft Print to PDF" conclui com status "entregue à impressora".
- [ ] Botão "Copiar diagnóstico completo" copia o relatório formatado para a área de transferência.
- [ ] Botão "Abrir pasta de logs" abre o diretório contendo o log diário em UTF-8.

### Checklist B: Conexão Remota (Terminal Services / RDP)
- [ ] Executável inicia na sessão do usuário remoto sem erros de RPC ou Spooler.
- [ ] Banner superior identifica corretamente a sessão RDP: `Sessão: RDP (RDP-Tcp#X, Cliente: ESTACAO-LOCAL)`.
- [ ] Grade de impressoras identifica as impressoras redirecionadas da estação do cliente (coluna `Redir. RDP = SIM`).
- [ ] Validação do sufixo dinâmico de impressoras RDP (ex.: `"HP LaserJet (redirecionado 1)"`): se o nome mudar entre reconexões, o teste alerta sobre impressora padrão inválida (Erro Win32 1801).
- [ ] Verificação se portas `TSxxx` ou portas de rede redirecionadas mantêm status "Pronta" mesmo sem retorno bidirecional, destacando o lembrete da Etapa 9.
- [ ] Teste de gravação de logs na pasta de perfil do usuário remoto (`%LOCALAPPDATA%` do servidor de terminal).
