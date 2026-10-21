using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;

namespace Chucao.Services;

public sealed class ListFileService
{
    private const string FilePrefix = "list-";
    private static readonly UTF8Encoding Utf8 = new(false, true);
    private readonly string directoryPath;

    public ListFileService(string? directoryPath = null)
    {
        this.directoryPath = directoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Chucao", "Lists");
    }

    public (IReadOnlyList<string> Names, IReadOnlyList<string> SkippedFiles) LoadLists()
    {
        Directory.CreateDirectory(directoryPath);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skippedFiles = new List<string>();
        foreach (string path in Directory.EnumerateFiles(directoryPath, "*.csv").Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                string name = DecodeName(Path.GetFileNameWithoutExtension(path));
                ValidateCsv(path);
                if (!names.Add(name))
                    skippedFiles.Add(Path.GetFileName(path));
            }
            catch (Exception exception) when (IsFileError(exception))
            {
                // One invalid file must not prevent the other lists from loading.
                skippedFiles.Add(Path.GetFileName(path));
            }
        }

        return (names.Order(StringComparer.OrdinalIgnoreCase).ToArray(), skippedFiles);
    }

    public void CreateList(string name)
    {
        ValidateName(name);
        Directory.CreateDirectory(directoryPath);
        // Check decoded names too, so names differing only in case are duplicates.
        foreach (string existingPath in Directory.EnumerateFiles(directoryPath, "*.csv"))
        {
            string existingName;
            try { existingName = DecodeName(Path.GetFileNameWithoutExtension(existingPath)); }
            catch (Exception exception) when (IsFileError(exception)) { continue; }
            if (StringComparer.OrdinalIgnoreCase.Equals(existingName, name))
                throw new DuplicateListException();
        }

        string path = GetFilePath(name);
        string temporaryPath = Path.Combine(directoryPath, $"{Guid.NewGuid():N}.tmp");
        try
        {
            using (var writer = new StreamWriter(temporaryPath, false, Utf8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteField("Concept");
                csv.WriteField("SeenOrder");
                csv.NextRecord();
            }

            // Never replace an existing list, even if it is invalid or was just created elsewhere.
            try { File.Move(temporaryPath, path, overwrite: false); }
            catch (IOException) when (File.Exists(path)) { throw new DuplicateListException(); }
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    public void DeleteList(string name)
    {
        ValidateName(name);
        File.Delete(GetFilePath(name));
    }

    // UTF-8 bytes encoded as hexadecimal form a safe, reversible Windows filename.
    // This preserves arbitrary display names without metadata columns or a separate catalog.
    private string GetFilePath(string name) => Path.Combine(directoryPath,
        FilePrefix + Convert.ToHexString(Utf8.GetBytes(name)) + ".csv");

    private static string DecodeName(string fileName)
    {
        if (!fileName.StartsWith(FilePrefix, StringComparison.Ordinal))
            throw new InvalidDataException("Unrecognized list filename.");
        string name = Utf8.GetString(Convert.FromHexString(fileName[FilePrefix.Length..]));
        ValidateName(name);
        return name;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 30 || name != name.Trim())
            throw new ArgumentException("List names must contain between 1 and 30 characters without outer whitespace.", nameof(name));
    }

    private static void ValidateCsv(string path)
    {
        using var reader = new StreamReader(path, Utf8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        if (!csv.Read())
            throw new InvalidDataException("Missing list header.");
        csv.ReadHeader();
        if (csv.HeaderRecord is not ["Concept", "SeenOrder"])
            throw new InvalidDataException("Unexpected list columns.");

        var seenOrders = new HashSet<int>();
        while (csv.Read())
        {
            if (csv.Parser.Count != 2 || string.IsNullOrWhiteSpace(csv.GetField("Concept"))
                || !int.TryParse(csv.GetField("SeenOrder"), NumberStyles.None, CultureInfo.InvariantCulture, out int order)
                || order < 0 || (order > 0 && !seenOrders.Add(order)))
                throw new InvalidDataException("Invalid concept or selection order.");
        }
    }

    public static bool IsFileError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or CsvHelperException or ArgumentException or FormatException;
}

public sealed class DuplicateListException : IOException
{
    public DuplicateListException() : base("A list with this name already exists.") { }
}
