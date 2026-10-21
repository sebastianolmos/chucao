using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;

namespace Chucao.Services;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private readonly Dictionary<string, Translation> translations;
    private string currentLanguage = "en";

    public LocalizationService()
    {
        using Stream stream = typeof(LocalizationService).Assembly
            .GetManifestResourceStream("Chucao.Resources.Translations.csv")
            ?? throw new InvalidOperationException("Translation resource is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        translations = csv.GetRecords<Translation>().ToDictionary(row => row.Key);
    }

    public string CurrentLanguage
    {
        get => currentLanguage;
        set
        {
            if (value is not ("en" or "es"))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (value == currentLanguage)
                return;

            currentLanguage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentLanguage)));
            // WPF refreshes all indexer bindings when Item[] changes.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        }
    }

    public string this[string key] => CurrentLanguage == "es"
        ? translations[key].Spanish
        : translations[key].English;

    public event PropertyChangedEventHandler? PropertyChanged;

    private sealed class Translation
    {
        public string Key { get; set; } = "";
        [Name("es")]
        public string Spanish { get; set; } = "";
        [Name("en")]
        public string English { get; set; } = "";
    }
}
