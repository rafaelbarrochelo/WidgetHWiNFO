using System.IO;

namespace WidgetHWiNFO.Services;

/// <summary>Tiny append-only error log in %LOCALAPPDATA%\WidgetHWiNFO\log.txt (capped at 512 KB).</summary>
internal static class Log
{
    private static readonly object Gate = new();

    public static string Directory { get; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WidgetHWiNFO");

    private static string FilePath => Path.Combine(Directory, "log.txt");

    public static void Error(string context, Exception ex) => Write($"ERROR {context}: {ex}");

    public static void Info(string message) => Write($"INFO  {message}");

    private static void Write(string line)
    {
        try
        {
            lock (Gate)
            {
                System.IO.Directory.CreateDirectory(Directory);
                var file = new FileInfo(FilePath);
                if (file.Exists && file.Length > 512 * 1024) file.Delete();
                File.AppendAllText(FilePath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {line}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never take the app down.
        }
    }
}
