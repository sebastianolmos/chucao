using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;

namespace Chucao.Services;

public sealed class SettingsService
{
    private readonly string filePath;

    public SettingsService(string? filePath = null)
    {
        this.filePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Chucao", "settings.csv");
    }

    public string LoadLanguage() => ReadValues()["Language"];

    public int LoadTimerMinutes()
    {
        var values = ReadValues();
        if (!values.TryGetValue("TimerMinutes", out string? text))
            return 1;
        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int minutes)
            || minutes is < 1 or > 59)
            throw new InvalidDataException("Unsupported timer duration.");
        return minutes;
    }

    private Dictionary<string, string> ReadValues()
    {
        if (!File.Exists(filePath))
            return new Dictionary<string, string> { ["Language"] = "en" };

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

        return loadedValues;
    }

    public void SaveLanguage(string language)
    {
        if (language is not ("en" or "es"))
            throw new ArgumentOutOfRangeException(nameof(language));

        var updatedValues = ReadValues();
        updatedValues["Language"] = language;
        SaveValues(updatedValues);
    }

    public void SaveTimerMinutes(int minutes)
    {
        if (minutes is < 1 or > 59)
            throw new ArgumentOutOfRangeException(nameof(minutes));
        var updatedValues = ReadValues();
        updatedValues["TimerMinutes"] = minutes.ToString(CultureInfo.InvariantCulture);
        SaveValues(updatedValues);
    }

    private void SaveValues(Dictionary<string, string> updatedValues)
    {
        string directory = Path.GetDirectoryName(Path.GetFullPath(filePath))!;
        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, $"{Guid.NewGuid():N}.tmp");
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
