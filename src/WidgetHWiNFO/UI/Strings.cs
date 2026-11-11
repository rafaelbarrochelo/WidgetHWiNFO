using System.Globalization;

namespace WidgetHWiNFO.UI;

/// <summary>Minimal localization: Portuguese when the Windows UI language is Portuguese, English otherwise.</summary>
public static class Strings
{
    private static readonly bool Pt = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt";

    private static string T(string pt, string en) => Pt ? pt : en;

    public static string AppName => "WidgetHWiNFO";

    // Sections
    public static string Cpu => "CPU";
    public static string Gpu => "GPU";
    public static string Memory => T("Memória", "Memory");
    public static string Disks => T("Discos", "Disks");
    public static string Network => T("Rede", "Network");
    public static string FansAndBoard => T("Ventoinhas e placa", "Fans & board");
    public static string SystemSection => T("Sistema", "System");
    public static string Motherboard => T("Placa-mãe", "Motherboard");
    public static string Uptime => T("Ligado há", "Uptime");
    public static string ProcessesCount => T("Processos", "Processes");
    public static string Handles => "Handles";
    public static string Duration(TimeSpan t) =>
        t.TotalDays >= 1 ? $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m"
        : t.TotalHours >= 1 ? $"{t.Hours}h {t.Minutes}m"
        : $"{t.Minutes}m";

    // Metric labels
    public static string Usage => T("Uso", "Usage");
    public static string Temperature => T("Temp.", "Temp");
    public static string HotSpot => "Hot spot";
    public static string Power => T("Consumo", "Power");
    public static string Clock => "Clock";
    public static string Vram => "VRAM";
    public static string Fan => T("Ventoinha", "Fan");
    public static string Download => "Download";
    public static string Upload => "Upload";
    public static string Life => T("vida", "life");
    public static string Of => T("de", "of");
    public static string FreeOf => T("livres de", "free of");
    public static string InUse => T("Em uso", "In use");
    public static string Cached => T("Em cache", "Cached");
    public static string Free => T("Livre", "Free");
    public static string Committed => T("Confirmada", "Committed");
    public static string LocalIp => T("IP local", "Local IP");
    public static string Threads => T("Threads", "Threads");
    public static string TopCpu => T("Mais usando CPU", "Top CPU");
    public static string TopMemory => T("Mais usando memória", "Top memory");
    public static string ShowProcesses => T("Mostrar processos", "Show processes");
    public static string HideProcesses => T("Ocultar processos", "Hide processes");
    public static string ArrangePanel => T("Organizar painel", "Arrange panel");
    public static string ArrangeHint => T(
        "Marque o que deve aparecer e use as setas para mudar a ordem. A coluna vale para o layout largo (Automática = coluna mais curta).",
        "Check what to show and use the arrows to reorder. The column applies to the wide layout (Auto = shorter column).");
    public static string ColumnAuto => T("Automática", "Auto");
    public static string ColumnLeft => T("Esquerda", "Left");
    public static string ColumnRight => T("Direita", "Right");
    public static string ColumnHint => T("Coluna no layout largo", "Column in the wide layout");
    public static string MoveUp => T("Mover para cima", "Move up");
    public static string MoveDown => T("Mover para baixo", "Move down");
    public static string ResetDefault => T("Restaurar padrão", "Reset to default");

    public static string CardName(Services.CardId id) => id switch
    {
        Services.CardId.Cpu => Cpu,
        Services.CardId.Gpu => Gpu,
        Services.CardId.Memory => Memory,
        Services.CardId.Disks => Disks,
        Services.CardId.Network => Network,
        Services.CardId.Fans => FansAndBoard,
        _ => SystemSection,
    };

    public static string PanelLayout => T("Layout do painel", "Panel layout");
    public static string LayoutCompact => T("Compacto (uma coluna)", "Compact (one column)");
    public static string LayoutWide => T("Largo (duas colunas)", "Wide (two columns)");
    public static string ProcessesStartCollapsed => T("Abrir com a lista de processos recolhida", "Open with process lists collapsed");

    // Status
    public static string Connected => T("Conectado ao HWiNFO", "Connected to HWiNFO");
    public static string Connecting => T("Conectando…", "Connecting…");
    public static string NotRunningTitle => T("HWiNFO não encontrado", "HWiNFO not found");
    public static string NotRunningDetail => T(
        "Abra o HWiNFO e ative em Configurações → \"Shared Memory Support\". A janela de Sensores precisa estar aberta (pode ficar minimizada).",
        "Open HWiNFO and enable Settings → \"Shared Memory Support\". The Sensors window must be open (it can be minimized).");
    public static string InactiveTitle => T("Memória compartilhada desativada", "Shared memory disabled");
    public static string InactiveDetail => T(
        "A versão gratuita do HWiNFO desliga a memória compartilhada após 12 horas. Reative-a nas configurações do HWiNFO ou reinicie-o.",
        "The free version of HWiNFO turns shared memory off after 12 hours. Re-enable it in HWiNFO settings or restart HWiNFO.");
    public static string StaleTitle => T("Dados parados", "Data is not updating");
    public static string StaleDetail => T(
        "O HWiNFO parou de atualizar os sensores. Verifique se a janela de Sensores está aberta e não pausada (ou o limite de 12h da versão gratuita).",
        "HWiNFO stopped updating sensors. Make sure the Sensors window is open and not paused (or the free version 12h limit was reached).");
    public static string InvalidTitle => T("Formato não reconhecido", "Unrecognized data");
    public static string InvalidDetail => T(
        "Não foi possível ler os dados do HWiNFO. Atualize o HWiNFO para a versão mais recente.",
        "Could not read HWiNFO data. Update HWiNFO to the latest version.");

    // Menu / settings
    public static string Open => T("Abrir", "Open");
    public static string Settings => T("Configurações", "Settings");
    public static string Exit => T("Sair", "Exit");
    public static string Save => T("Salvar", "Save");
    public static string Cancel => T("Cancelar", "Cancel");
    public static string Sections => T("Seções do painel", "Panel sections");
    public static string General => T("Geral", "General");
    public static string UpdateInterval => T("Intervalo de atualização", "Update interval");
    public static string StartWithWindows => T("Iniciar com o Windows", "Start with Windows");
    public static string PinTrayIcons => T("Sempre mostrar o ícone na barra de tarefas", "Always show the icon on the taskbar");
    public static string HideStoppedFans => T("Ocultar ventoinhas paradas (0 RPM)", "Hide stopped fans (0 RPM)");
    public static string Seconds(double s) => s < 1 ? $"{s * 1000:0} ms" : T($"{s:0.#} segundo{(s == 1 ? "" : "s")}", $"{s:0.#} second{(s == 1 ? "" : "s")}");
    public static string AdvancedHint => T(
        "Avançado: para trocar o sensor usado em uma métrica, edite \"SensorOverrides\" no arquivo de configurações.",
        "Advanced: to change which sensor a metric uses, edit \"SensorOverrides\" in the settings file.");
    public static string OpenSettingsFile => T("Abrir arquivo de configurações", "Open settings file");
}
