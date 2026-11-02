using System.Globalization;
using System.IO;
using System.Text;
using CsvHelper;
using Chucao.Models;

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

    public IReadOnlyList<ConceptEntry> LoadConcepts(string listName)
    {
        ValidateName(listName);
        return ReadConcepts(GetFilePath(listName));
    }

    public IReadOnlyList<ConceptEntry> AddConcept(string listName, string concept)
    {
        if (string.IsNullOrWhiteSpace(concept) || concept.Length > 30)
            throw new ArgumentException("Concepts must contain between 1 and 30 characters.", nameof(concept));

        var concepts = LoadConcepts(listName).ToList();
        if (concepts.Any(entry => string.Equals(entry.Concept, concept, StringComparison.Ordinal)))
            throw new DuplicateConceptException();

        concepts.Add(new ConceptEntry(concept, 0));
        SaveConcepts(listName, concepts);
        return concepts;
    }

    public IReadOnlyList<ConceptEntry> ResetList(string listName)
    {
        var concepts = LoadConcepts(listName)
            .Select(entry => entry with { SeenOrder = 0 }).ToArray();
        SaveConcepts(listName, concepts);
        return concepts;
    }

    public IReadOnlyList<ConceptEntry> MarkConceptAsSeen(string listName, ConceptEntry concept)
    {
        var concepts = LoadConcepts(listName).ToList();
        int index = concepts.FindIndex(entry => entry == concept);
        if (index < 0)
            throw new IOException("The concept has changed or no longer exists.");
        if (concepts[index].SeenOrder > 0)
            return concepts;

        int lastOrder = concepts.Max(entry => entry.SeenOrder);
        if (lastOrder == int.MaxValue)
            throw new InvalidDataException("The selection order limit has been reached.");
        concepts[index] = concepts[index] with { SeenOrder = lastOrder + 1 };
        SaveConcepts(listName, concepts);
        return concepts;
    }

    public IReadOnlyList<ConceptEntry> DeleteConcept(string listName, ConceptEntry concept)
    {
        var concepts = LoadConcepts(listName).ToList();
        int index = concepts.FindIndex(entry => entry == concept);
        if (index < 0)
            throw new IOException("The concept has changed or no longer exists.");
        // Remove only the chosen row, including in legacy files with duplicate text.
        concepts.RemoveAt(index);
        SaveConcepts(listName, concepts);
        return concepts;
    }

    private void SaveConcepts(string listName, IReadOnlyList<ConceptEntry> concepts)
    {
        string path = GetFilePath(listName);
        string temporaryPath = Path.Combine(directoryPath, $"{Guid.NewGuid():N}.tmp");
        try
        {
            using (var writer = new StreamWriter(temporaryPath, false, Utf8))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteField("Concept");
                csv.WriteField("SeenOrder");
                csv.NextRecord();
                foreach (var entry in concepts)
                {
                    csv.WriteField(entry.Concept);
                    csv.WriteField(entry.SeenOrder);
                    csv.NextRecord();
                }
            }

            // Replace an existing file only after writing the complete CSV.
            // If it was deleted or is locked, fail without changing the displayed state.
            File.Replace(temporaryPath, path, destinationBackupFileName: null);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
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

    private static void ValidateCsv(string path) => _ = ReadConcepts(path);

    private static IReadOnlyList<ConceptEntry> ReadConcepts(string path)
    {
        using var reader = new StreamReader(path, Utf8);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        if (!csv.Read())
            throw new InvalidDataException("Missing list header.");
        csv.ReadHeader();
        if (csv.HeaderRecord is not ["Concept", "SeenOrder"])
            throw new InvalidDataException("Unexpected list columns.");

        var seenOrders = new HashSet<int>();
        var concepts = new List<ConceptEntry>();
        while (csv.Read())
        {
            if (csv.Parser.Count != 2 || string.IsNullOrWhiteSpace(csv.GetField("Concept"))
                || !int.TryParse(csv.GetField("SeenOrder"), NumberStyles.None, CultureInfo.InvariantCulture, out int order)
                || order < 0 || (order > 0 && !seenOrders.Add(order)))
                throw new InvalidDataException("Invalid concept or selection order.");
            concepts.Add(new ConceptEntry(csv.GetField("Concept")!, order));
        }
        return concepts;
    }

    public static bool IsFileError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or CsvHelperException or ArgumentException or FormatException;
}

public sealed class DuplicateListException : IOException
{
    public DuplicateListException() : base("A list with this name already exists.") { }
}

public sealed class DuplicateConceptException : IOException
{
    public DuplicateConceptException() : base("This exact concept already exists in the list.") { }
}
