using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WidgetHWiNFO.Services;

public enum CardId { Cpu, Gpu, Memory, Disks, Network, Fans, System }

/// <summary>Column used by a card in the wide layout. Auto = whichever column is shorter.</summary>
public enum CardColumn { Auto, Left, Right }

public sealed class CardLayout
{
    public CardId Id { get; set; }
    public bool Visible { get; set; } = true;
    public CardColumn Column { get; set; }

    public CardLayout Clone() => new() { Id = Id, Visible = Visible, Column = Column };
}

public sealed class AppSettings
{
    public int UpdateIntervalMs { get; set; } = 1000;

    public bool HideStoppedFans { get; set; } = true;

    /// <summary>Keep the tray icon visible on the taskbar instead of the Windows 11 overflow menu.</summary>
    public bool PinTrayIcons { get; set; } = true;

    /// <summary>Panel cards in display order, with visibility and wide-layout column.</summary>
    public List<CardLayout> Cards { get; set; } = DefaultCards();

    public static List<CardLayout> DefaultCards() =>
    [
        new() { Id = CardId.Cpu, Column = CardColumn.Left },
        new() { Id = CardId.Gpu, Column = CardColumn.Right },
        new() { Id = CardId.Memory, Column = CardColumn.Left },
        new() { Id = CardId.Disks, Column = CardColumn.Right },
        new() { Id = CardId.Network, Column = CardColumn.Right },
        new() { Id = CardId.Fans, Column = CardColumn.Right },
        new() { Id = CardId.System, Column = CardColumn.Auto },
    ];

    public bool IsVisible(CardId id) => Cards.FirstOrDefault(c => c.Id == id)?.Visible ?? true;

    // Pre-"Cards" settings files stored one bool per section; read them once, never write them.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowCpu { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowGpu { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowMemory { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowDisks { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowNetwork { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowFans { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] public bool? ShowSystem { get; set; }

    /// <summary>Open the panel with the top-process lists collapsed (they are not sampled while collapsed).</summary>
    public bool ProcessesStartCollapsed { get; set; }

    /// <summary>Two-column panel instead of the compact single column.</summary>
    public bool WideLayout { get; set; }

    /// <summary>
    /// Advanced: force a specific HWiNFO label for a metric, e.g. { "cpu.temp": "CPU (Tctl)" }.
    /// Keys: cpu.temp, cpu.usage, cpu.power, gpu.temp, gpu.usage, gpu.power.
    /// </summary>
    public Dictionary<string, string> SensorOverrides { get; set; } = [];

    // ---- Persistence ----------------------------------------------------------------------

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WidgetHWiNFO", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions);
                if (s is not null) return s.Normalize();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Loading settings (using defaults)", ex);
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            string tmp = FilePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(this, JsonOptions));
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch (Exception ex)
        {
            Log.Error("Saving settings", ex);
        }
    }

    private AppSettings Normalize()
    {
        UpdateIntervalMs = Math.Clamp(UpdateIntervalMs, 500, 10_000);
        SensorOverrides ??= [];

        // Cards: drop unknown/duplicate entries, append any card missing (e.g. added in a newer version).
        var cards = (Cards ?? []).Where(c => c is not null && Enum.IsDefined(c.Id)).DistinctBy(c => c.Id).ToList();
        foreach (var d in DefaultCards())
            if (!cards.Any(c => c.Id == d.Id)) cards.Add(d);
        Cards = cards;

        // Migrate legacy per-section flags.
        void Migrate(CardId id, bool? legacy)
        {
            if (legacy is { } v) cards.First(c => c.Id == id).Visible = v;
        }
        Migrate(CardId.Cpu, ShowCpu);
        Migrate(CardId.Gpu, ShowGpu);
        Migrate(CardId.Memory, ShowMemory);
        Migrate(CardId.Disks, ShowDisks);
        Migrate(CardId.Network, ShowNetwork);
        Migrate(CardId.Fans, ShowFans);
        Migrate(CardId.System, ShowSystem);
        ShowCpu = ShowGpu = ShowMemory = ShowDisks = ShowNetwork = ShowFans = ShowSystem = null;

        return this;
    }
}
