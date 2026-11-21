using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace CatGuard;

internal sealed class UserSettings
{
    public bool Enabled { get; set; } // session only; always starts off
    public int Sensitivity { get; set; } = 5;
    public bool WarnOnly { get; set; }
    public bool StayLocked { get; set; }
    public bool Sound { get; set; }
    public bool StartWithWindows { get; set; }
    private static string PathName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WindowsCatGuard", "settings.json");
    public static UserSettings Load()
    {
        try
        {
            var result = JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(PathName)) ?? new();
            result.Enabled = false;
            result.Sensitivity = Math.Clamp(result.Sensitivity, 1, 10);
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
            result.StartWithWindows = key?.GetValue("WindowsCatGuard") is not null;
            return result;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
        var temp = PathName + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, PathName, true);
    }
    public static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("WindowsCatGuard", $"\"{Environment.ProcessPath}\" --tray");
        else key.DeleteValue("WindowsCatGuard", false);
    }
}
