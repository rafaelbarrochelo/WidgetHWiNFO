# WidgetHWiNFO

Monitor minimalista de hardware na bandeja do Windows, no estilo do app *Stats* do macOS, usando os sensores do [HWiNFO](https://www.hwinfo.com/).

*[English below](#english)*

- **Um único ícone discreto na bandeja**: ao passar o mouse, mostra CPU, GPU, RAM e rede ao vivo.
- **Painel com um clique**: CPU (com uso por thread), GPU, memória (em uso / em cache / livre e memória confirmada), discos (espaço livre, temperatura, vida útil, leitura/escrita), rede (download/upload e IP local) e ventoinhas/placa-mãe, com mini-gráficos do último minuto.
- **Processos que mais consomem** CPU e memória (agrupados por programa, como "chrome (22)"). Só são coletados enquanto o painel está aberto e podem ser ocultados.
- **Card Sistema**: versão do Windows, placa-mãe, tempo ligado e contagem de processos/threads/handles.
- **Painel personalizável**: layout compacto (uma coluna) ou largo (duas colunas). Escolha quais cards aparecem, em que ordem e em qual coluna.
- **Leve**: ~0,05% de CPU e ~45 MB de memória privada em repouso (~0,1% com o painel aberto). Não usa navegador embutido e não depende de pacotes de terceiros.
- **Confiável**: só lê a memória compartilhada do HWiNFO (não precisa de administrador nem de driver), reconecta sozinho quando o HWiNFO reinicia e avisa quando os dados param.
- **Visual nativo do Windows 11**: fundo acrílico, cantos arredondados, tema claro/escuro automático. Interface em português ou inglês, conforme o idioma do Windows.

## Requisitos

- Windows 10 ou 11 (x64)
- HWiNFO64 com **Shared Memory Support** ativado

## Instalação

1. Baixe o `.zip` mais recente em **Releases**:
   - `WidgetHWiNFO-x.y.z-win-x64-portable.zip`: roda direto, sem instalar nada (~67 MB).
   - `WidgetHWiNFO-x.y.z-win-x64.zip`: bem menor (<1 MB), mas exige o [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0).
2. Extraia em uma pasta qualquer e execute `WidgetHWiNFO.exe`.
3. (Opcional) Em ⚙ Configurações, marque **Iniciar com o Windows**.

## Configurando o HWiNFO

1. Abra o HWiNFO64 → **Settings** (ou o ícone de engrenagem na janela *Sensors*).
2. Marque **Shared Memory Support**.
3. Deixe a janela **Sensors** aberta (pode ficar minimizada). Para iniciar tudo junto, marque também *Auto Start*, *Minimize Sensors on Startup* e *Show Sensors on Startup*.

> **Versão gratuita do HWiNFO:** a memória compartilhada é desligada automaticamente depois de **12 horas**. O WidgetHWiNFO detecta isso e avisa. Basta reativar a opção ou reiniciar o HWiNFO. A versão Pro não tem esse limite.

## Uso

- **Passar o mouse** sobre o ícone: resumo rápido (CPU, GPU, RAM, rede).
- **Clique esquerdo**: abre/fecha o painel.
- **Clique direito**: menu (Abrir, Configurações, Sair).
- `Esc` ou clicar fora fecha o painel.

### Organizando o painel

Em ⚙ **Configurações → Organizar painel**:

- **Layout do painel**: *Compacto* (uma coluna, 360 px) ou *Largo* (duas colunas, 700 px).
- Para cada card: marque para exibir, use ↑ ↓ para ordenar e escolha a coluna do layout largo (*Esquerda*, *Direita* ou *Automática*, que coloca o card na coluna mais curta).
- **Abrir com a lista de processos recolhida**: com a lista recolhida, os processos nem são coletados.
- **Restaurar padrão** volta para o arranjo original.

### Escolhendo outro sensor (avançado)

O app escolhe automaticamente os sensores mais adequados (por exemplo `CPU (Tctl/Tdie)` em AMD e `CPU Package` em Intel). Para forçar outro sensor, edite `%APPDATA%\WidgetHWiNFO\settings.json` (ou use *Abrir arquivo de configurações*) usando o nome do sensor como aparece no HWiNFO:

```json
"SensorOverrides": {
  "cpu.temp": "CPU (Tctl)",
  "gpu.temp": "GPU Hot Spot Temperature"
}
```

Chaves disponíveis: `cpu.temp`, `cpu.usage`, `cpu.power`, `gpu.temp`, `gpu.usage`, `gpu.power`. Reinicie o app depois de editar.

## Solução de problemas

| Problema | Solução |
|---|---|
| "HWiNFO não encontrado" | Abra o HWiNFO, ative *Shared Memory Support* e deixe a janela *Sensors* aberta. |
| "Dados parados" | A janela *Sensors* foi fechada ou pausada, ou o limite de 12h da versão gratuita foi atingido. |
| Ícone não aparece na barra | Ative *Sempre mostrar o ícone na barra de tarefas* nas configurações, ou vá em Configurações do Windows → Personalização → Barra de tarefas → Outros ícones da bandeja do sistema. |
| Algum valor aparece como "—" | Seu hardware não expõe esse sensor, ou ele tem outro nome. Veja *Escolhendo outro sensor*. |

O log de erros fica em `%LOCALAPPDATA%\WidgetHWiNFO\log.txt`.

## Compilando

Requer o [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```powershell
dotnet test tests/WidgetHWiNFO.Tests
dotnet run --project src/WidgetHWiNFO              # --show abre o painel ao iniciar
dotnet publish src/WidgetHWiNFO -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Para publicar uma versão, crie uma tag (`git tag v0.1.0 && git push --tags`). O GitHub Actions roda os testes, gera os dois `.zip` com `SHA256SUMS.txt` e cria a Release.

### Próximos passos

- Reorganizar os cards arrastando e soltando direto no painel.
- Fonte de dados alternativa sem HWiNFO (uso de CPU/GPU/RAM, disco e rede via APIs do Windows), mantendo o HWiNFO para temperaturas e ventoinhas.

### Estrutura

```
src/WidgetHWiNFO.Core    leitura da memória compartilhada do HWiNFO + tradução para métricas (sem UI, testável);
                         Windows/: processos, memória detalhada, volumes e IP via APIs do Windows
src/WidgetHWiNFO         app WPF: ícones da bandeja, painel e configurações
tests/                   testes xUnit com buffers sintéticos (AMD/Intel/NVIDIA) + teste ao vivo opcional
```

---

## English

Minimal Windows tray hardware monitor, in the spirit of macOS *Stats*, powered by [HWiNFO](https://www.hwinfo.com/) sensors.

- A single, unobtrusive tray icon; hover it for live CPU/GPU/RAM/network values.
- One-click panel showing CPU (per-thread bars), GPU, memory breakdown, disks (free space, temps, health), network, fans, motherboard and system info, with 60-second sparklines.
- Top processes by CPU and memory, sampled only while the panel is open.
- Customizable: compact or wide (two-column) layout. Choose which cards appear, their order and their column.
- Lightweight: about 0.05% CPU and 45 MB private memory when idle. No embedded browser and no third-party packages.
- Reads HWiNFO shared memory only (no admin, no driver). Reconnects automatically and warns you when data stops updating.

**Setup:** in HWiNFO64 enable *Settings → Shared Memory Support* and keep the *Sensors* window open (it can be minimized). The free version of HWiNFO turns shared memory off after 12 hours.

**Download:** grab the `-portable.zip` (self-contained) or the small `.zip` (requires .NET 10 Desktop Runtime) from Releases.

HWiNFO is a trademark of its respective owner. This project is not affiliated with HWiNFO.

## License

[MIT](LICENSE)
