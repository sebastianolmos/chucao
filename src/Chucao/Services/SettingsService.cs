using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;

namespace Chucao.Services;

public sealed class SettingsService
{
    private readonly string filePath;
    private Dictionary<string, string> values = new();

    public SettingsService(string? filePath = null)
    {
        this.filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Chucao", "settings.csv");
    }

    public string LoadLanguage()
    {
        if (!File.Exists(filePath))
            return "en";

        using var reader = new StreamReader(filePath, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        var loadedValues = new Dictionary<string, string>();
        foreach (var row in csv.GetRecords<Setting>())
        {
            if (!loadedValues.TryAdd(row.Key, row.Value))
                throw new InvalidDataException("Duplicate settings key.");
        }

        if (!loadedValues.TryGetValue("Language", out string? language)
            || language is not ("en" or "es"))
            throw new InvalidDataException("Missing or unsupported language preference.");

        values = loadedValues;
        return language;
    }

    public void SaveLanguage(string language)
    {
        if (language is not ("en" or "es"))
            throw new ArgumentOutOfRangeException(nameof(language));

        string directory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");
        var updatedValues = new Dictionary<string, string>(values) { ["Language"] = language };
        try
        {
            using (var writer = new StreamWriter(temporaryPath, false, new UTF8Encoding(false)))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(updatedValues.Select(pair => new Setting
                {
                    Key = pair.Key,
                    Value = pair.Value
                }));
            }

            // Replace only after the complete CSV has been written successfully.
            File.Move(temporaryPath, filePath, overwrite: true);
            values = updatedValues;
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private sealed class Setting
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
    }
}
