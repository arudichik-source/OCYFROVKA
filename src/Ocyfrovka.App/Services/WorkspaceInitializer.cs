using System.IO;
using System.Text.Json;

namespace Ocyfrovka.App.Services;

internal static class WorkspaceInitializer
{
    private static readonly string[] RelativeDirectories =
    [
        "Engine/OCR/tessdata",
        "Data/Dictionaries",
        "Data/Templates",
        "Workspace/Input",
        "Workspace/Temp",
        "Workspace/Processed",
        "Export",
        "Logs",
        "Config"
    ];

    public static void EnsurePortableLayout()
    {
        var root = AppContext.BaseDirectory;

        foreach (var relativePath in RelativeDirectories)
        {
            Directory.CreateDirectory(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        }

        var settingsPath = Path.Combine(root, "Config", "settings.json");
        if (!File.Exists(settingsPath))
        {
            var settings = new
            {
                schemaVersion = 1,
                ocrLanguage = "ukr",
                ocrProfile = "auto",
                autoDeskew = true,
                autoClearTemp = true
            };

            File.WriteAllText(
                settingsPath,
                JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}