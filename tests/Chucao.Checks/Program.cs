using Chucao.Services;
using Chucao.ViewModels;

// Small executable checks, with no test framework dependency or user-data access.
string directory = Path.Combine(Path.GetTempPath(), "Chucao.Checks", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
string path = Path.Combine(directory, "settings.csv");
int passed = 0;

void Check(bool condition, string name)
{
    if (!condition)
        throw new InvalidOperationException(name);
    Console.WriteLine($"PASS: {name}");
    passed++;
}

try
{
    var settings = new SettingsService(path);
    Check(settings.LoadLanguage() == "en" && !File.Exists(path), "First launch defaults to English without changing files");
    settings.SaveLanguage("es");
    Check(new SettingsService(path).LoadLanguage() == "es", "Spanish survives a new session");
    settings.SaveLanguage("en");
    Check(new SettingsService(path).LoadLanguage() == "en", "English survives a new session");

    File.WriteAllText(path, "Key,Value\nLanguage,es\nNote,\"Texto con coma, comillas \"\"y acentos\"\"\"\n");
    settings = new SettingsService(path);
    Check(settings.LoadLanguage() == "es", "CSV settings with quoted fields can be read");
    settings.SaveLanguage("en");
    Check(File.ReadAllText(path).Contains("Texto con coma, comillas \"\"y acentos\"\""), "Changing language preserves other settings");

    foreach (string invalid in new[] { "Key,Value\nLanguage,fr\n", "Key,Value\nLanguage,en\nLanguage,es\n", "Key,Value\n" })
    {
        File.WriteAllText(path, invalid);
        bool rejected = false;
        try { new SettingsService(path).LoadLanguage(); }
        catch (InvalidDataException) { rejected = true; }
        Check(rejected && File.ReadAllText(path) == invalid, "Invalid settings are rejected without changing the file");
    }

    File.WriteAllText(path, "Key,Value\nLanguage,en\n");
    settings = new SettingsService(path);
    settings.LoadLanguage();
    using (var locked = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        bool rejected = false;
        try { settings.SaveLanguage("es"); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { rejected = true; }
        Check(rejected, "A locked settings file reports a save failure");
    }
    Check(new SettingsService(path).LoadLanguage() == "en" && Directory.GetFiles(directory, "*.tmp").Length == 0,
        "A failed save preserves the previous preference and cleans up the temporary file");

    var localization = new LocalizationService();
    Check(localization["Window.Title"] == "Chucao" && localization["Practice.Subtitle"] == "Concept Practice" && localization["Language.Toggle"] == "ES",
        "English title and destination language are correct");
    localization.CurrentLanguage = "es";
    Check(localization["Window.Title"] == "Chucao" && localization["Practice.Subtitle"] == "Práctica de conceptos" && localization["Language.Toggle"] == "EN",
        "Spanish title and destination language are correct");
    localization.CurrentLanguage = "en";
    Check(localization["Language.ToggleDescription"] == "Switch to Español", "Language can be changed back in the same session");
    string listsDirectory = Path.Combine(directory, "Lists");
    var listFiles = new ListFileService(listsDirectory);
    var main = new MainViewModel(listFiles);
    var warnings = new List<string>();
    main.WarningRequested += (key, _) => warnings.Add(key);
    main.LoadLists();
    Check(main.Lists.Count == 0, "A new data folder has no sample lists");
    int initialCount = main.Lists.Count;
    main.CreateListCommand.Execute(null);
    main.DraftListName = "   ";
    main.ConfirmCreateCommand.Execute(null);
    Check(main.IsCreatingList && main.Lists.Count == initialCount, "A blank name does not create a list or close the form");
    main.DraftListName = new string('a', 31);
    main.ConfirmCreateCommand.Execute(null);
    Check(main.Lists.Count == initialCount, "Names exceeding 30 characters cannot be confirmed");
    main.DraftListName = "Lista cancelada";
    main.CancelDialogCommand.Execute(null);
    Check(!main.IsDialogOpen && main.Lists.Count == initialCount, "Canceling creation preserves the lists");

    string listName = new string('ñ', 30);
    main.CreateListCommand.Execute(null);
    main.DraftListName = listName;
    main.ConfirmCreateCommand.Execute(null);
    Check(main.Lists.Last() == listName && main.Lists.Count == initialCount + 1 && !main.IsDialogOpen,
        "A 30-character name creates a list and closes the form");
    string createdPath = Directory.GetFiles(listsDirectory, "*.csv").Single();
    Check(File.ReadAllText(createdPath).TrimEnd() == "Concept,SeenOrder", "Creating a list writes an empty concept CSV");
    var reopened = new MainViewModel(new ListFileService(listsDirectory));
    reopened.LoadLists();
    Check(reopened.Lists.SequenceEqual(main.Lists), "A new session recovers the saved lists");
    main.CreateListCommand.Execute(null);
    main.DraftListName = listName;
    main.ConfirmCreateCommand.Execute(null);
    Check(main.IsCreatingList && main.Lists.Count == initialCount + 1 && warnings.Last() == "Lists.DuplicateName",
        "A duplicate name keeps the creation form open and reports the conflict");
    main.CancelDialogCommand.Execute(null);
    main.OpenListCommand.Execute(listName);
    main.Localization.CurrentLanguage = "es";
    main.Localization.CurrentLanguage = "en";
    Check(main.SelectedList == listName && main.HasSelectedList && main.Lists.Contains(listName),
        "Changing language preserves the selected list and its name");
    main.BackCommand.Execute(null);
    Check(main.IsListsVisible && main.Lists.Contains(listName), "Returning from detail does not delete the list");
    main.OpenListCommand.Execute(listName);
    main.DeleteListCommand.Execute(null);
    main.CancelDialogCommand.Execute(null);
    Check(main.SelectedList == listName && main.Lists.Contains(listName) && !main.IsDialogOpen,
        "Canceling deletion preserves the list and detail view");
    Check(File.Exists(createdPath), "Canceling deletion preserves the CSV");
    main.DeleteListCommand.Execute(null);
    using (var locked = File.Open(createdPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        main.ConfirmDeleteCommand.Execute(null);
        Check(main.IsDeletingList && main.SelectedList == listName && main.Lists.Contains(listName)
            && warnings.Last() == "Lists.DeleteError", "A failed deletion preserves the list and confirmation dialog");
    }
    main.CancelDialogCommand.Execute(null);
    main.DeleteListCommand.Execute(null);
    main.ConfirmDeleteCommand.Execute(null);
    Check(!main.Lists.Contains(listName) && main.Lists.Count == initialCount && main.IsListsVisible && !main.IsDialogOpen,
        "Confirmed deletion removes the list and returns to the main view");
    Check(!File.Exists(createdPath) && new ListFileService(listsDirectory).LoadLists().Names.Count == 0,
        "Confirmed deletion removes the CSV and survives a restart");

    string unusualName = "Historia / \"XX\": ?*<>|\\";
    listFiles.CreateList(unusualName);
    string unusualPath = Directory.GetFiles(listsDirectory, "*.csv").Single();
    Check(Path.GetFileNameWithoutExtension(unusualPath) != unusualName && listFiles.LoadLists().Names.Contains(unusualName),
        "Internal filenames preserve display names containing Windows-reserved characters");
    string original = File.ReadAllText(unusualPath);
    bool duplicateRejected = false;
    try { listFiles.CreateList(unusualName.ToUpperInvariant()); }
    catch (DuplicateListException) { duplicateRejected = true; }
    Check(duplicateRejected && File.ReadAllText(unusualPath) == original,
        "Names differing only in case cannot overwrite an existing list");

    // Simulate concept data without implementing concept editing or practice.
    string conceptCsv = "Concept,SeenOrder\r\n\"Célula, tejido y \"\"órgano\"\"\",0\r\n\"Concepto\ncon salto\",2\r\n";
    File.WriteAllText(unusualPath, conceptCsv);
    Check(listFiles.LoadLists().Names.Contains(unusualName) && File.ReadAllText(unusualPath) == conceptCsv,
        "Quoted concepts, commas, accents and line breaks load without modifying progress");

    File.WriteAllText(Path.Combine(listsDirectory, "other.csv"), "Name,Value\nOther,1\n");
    var loaded = listFiles.LoadLists();
    Check(loaded.Names.Contains(unusualName) && loaded.SkippedFiles.Contains("other.csv"),
        "Unrecognized CSVs are skipped while valid lists load");
    foreach (string invalidCsv in new[]
    {
        "", "Other,Columns\n", "Concept,SeenOrder\nUno,-1\n", "Concept,SeenOrder\nUno,text\n",
        "Concept,SeenOrder\nUno,1\nDos,1\n", "Concept,SeenOrder\n,0\n", "Concept,SeenOrder\nUno,0,extra\n"
    })
    {
        File.WriteAllText(unusualPath, invalidCsv);
        var invalidLoad = listFiles.LoadLists();
        Check(!invalidLoad.Names.Contains(unusualName) && invalidLoad.SkippedFiles.Contains(Path.GetFileName(unusualPath))
            && File.ReadAllText(unusualPath) == invalidCsv, "An invalid list is reported and left unchanged");
    }
    File.WriteAllText(unusualPath, conceptCsv);
    main.LoadLists();
    Check(main.Lists.Contains(unusualName) && warnings.Last() == "Lists.InvalidFilesWarning",
        "The view model reports skipped files and still displays valid lists");
    using (var locked = File.Open(unusualPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
    {
        var lockedLoad = listFiles.LoadLists();
        Check(lockedLoad.SkippedFiles.Contains(Path.GetFileName(unusualPath)), "An unreadable list is reported without crashing startup");
    }

    string blockedDirectory = Path.Combine(directory, "blocked");
    File.WriteAllText(blockedDirectory, "This file prevents creating a directory at this path.");
    var blocked = new MainViewModel(new ListFileService(blockedDirectory));
    string? lastWarning = null;
    blocked.WarningRequested += (key, _) => lastWarning = key;
    blocked.LoadLists();
    Check(lastWarning == "Lists.LoadError", "An unavailable data folder reports a load error");
    blocked.CreateListCommand.Execute(null);
    blocked.DraftListName = "Test";
    blocked.ConfirmCreateCommand.Execute(null);
    Check(blocked.Lists.Count == 0 && blocked.IsCreatingList && blocked.DraftListName == "Test"
        && lastWarning == "Lists.CreateError", "A failed creation preserves the draft and does not add an unsaved card");
    Console.WriteLine($"{passed} checks passed.");
}
finally
{
    // Only remove the unique temporary directory created by this process.
    Directory.Delete(directory, recursive: true);
}
