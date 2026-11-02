using Chucao.Services;
using Chucao.ViewModels;

internal static class ConceptChecks
{
    public static void Run(string temporaryDirectory, Action<bool, string> check)
    {
        string directory = Path.Combine(temporaryDirectory, "ConceptChecks");
        var files = new ListFileService(directory);
        files.CreateList("Conceptos");
        string csvPath = Directory.GetFiles(directory, "*.csv").Single();
        var main = new MainViewModel(files);
        string? warning = null;
        main.WarningRequested += (key, _) => warning = key;
        main.LoadLists();
        main.OpenListCommand.Execute("Conceptos");
        check(main.Concepts.Count == 0, "An empty list opens with no concepts");

        void Add(string text)
        {
            main.AddConceptCommand.Execute(null);
            main.DraftConceptText = text;
            main.ConfirmAddConceptCommand.Execute(null);
        }

        Add("   ");
        check(main.IsCreatingConcept && main.Concepts.Count == 0, "A blank concept cannot be confirmed");
        main.DraftConceptText = new string('a', 31);
        main.ConfirmAddConceptCommand.Execute(null);
        check(main.IsCreatingConcept && main.Concepts.Count == 0, "Concepts over 30 characters cannot be confirmed");
        main.CancelDialogCommand.Execute(null);
        check(!main.IsDialogOpen && files.LoadConcepts("Conceptos").Count == 0, "Canceling concept creation leaves the CSV unchanged");

        string text = "Célula, \"unidad\"";
        Add(text);
        check(main.Concepts.Single().Concept == text && main.Concepts.Single().SeenOrder == 0,
            "A new concept is displayed with selection order zero");
        check(files.LoadConcepts("Conceptos").Single().Concept == text,
            "Quoted concepts with accents and commas survive CSV saving");
        check(main.AdditionNoticeCount == 1 && main.AdditionNoticeText == main.Localization["Concepts.AddedOne"],
            "Adding a single concept publishes the singular count notice");
        main.ClearAdditionNotice();
        string savedCsv = File.ReadAllText(csvPath);
        Add(text);
        check(main.IsCreatingConcept && main.DraftConceptText == text && warning == "Concepts.Duplicate"
            && File.ReadAllText(csvPath) == savedCsv && !main.HasAdditionNotice,
            "An exact duplicate is rejected without changing the CSV, losing the draft or announcing success");
        main.CancelDialogCommand.Execute(null);
        Add(text.ToLowerInvariant());
        check(main.Concepts.Count == 2, "Duplicate matching uses exact case-sensitive text");
        Add(new string('ñ', 30));
        check(main.Concepts.Count == 3 && !main.IsDialogOpen, "A 30-character concept can be saved");

        var reopened = new MainViewModel(new ListFileService(directory));
        reopened.LoadLists();
        reopened.OpenListCommand.Execute("Conceptos");
        check(reopened.Concepts.SequenceEqual(main.Concepts), "A new session recovers all saved concept texts and states");
        files.CreateList("Otra");
        files.AddConcept("Otra", text);
        check(files.LoadConcepts("Otra").Single().Concept == text, "The same concept may exist in different lists");

        // Seed existing progress; selection and rounds are not implemented by these checks.
        File.WriteAllText(csvPath, "Concept,SeenOrder\r\nUno,3\r\nDos,1\r\n");
        main.OpenListCommand.Execute("Conceptos");
        Add("Tres");
        check(files.LoadConcepts("Conceptos").Select(entry => entry.SeenOrder).SequenceEqual(new[] { 3, 1, 0 }),
            "Adding a concept preserves the progress of existing concepts");
        main.ResetListCommand.Execute(null);
        check(main.Concepts.Select(entry => entry.Concept).SequenceEqual(new[] { "Uno", "Dos", "Tres" })
            && files.LoadConcepts("Conceptos").All(entry => entry.SeenOrder == 0),
            "Resetting preserves text and order and saves zero for every concept");
        main.ResetListCommand.Execute(null);
        check(files.LoadConcepts("Conceptos").Count == 3, "Resetting an already unused list preserves its contents");
        files.CreateList("Vacía");
        check(files.ResetList("Vacía").Count == 0, "An empty list can be reset");

        savedCsv = File.ReadAllText(csvPath);
        using (var locked = File.Open(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            Add("No guardado");
            check(main.IsCreatingConcept && main.Concepts.Count == 3 && warning == "Concepts.SaveError",
                "A failed save preserves the draft and displayed concepts");
            main.CancelDialogCommand.Execute(null);
            main.ResetListCommand.Execute(null);
            check(warning == "Concepts.ResetError", "A failed reset reports a translated error key");
        }
        check(File.ReadAllText(csvPath) == savedCsv && Directory.GetFiles(directory, "*.tmp").Length == 0,
            "Failed writes preserve the original CSV and clean up temporary files");

        File.WriteAllText(csvPath, "Invalid,Header\n");
        bool rejected = false;
        try { files.ResetList("Conceptos"); }
        catch (InvalidDataException) { rejected = true; }
        check(rejected && File.ReadAllText(csvPath) == "Invalid,Header\n",
            "Resetting refuses to overwrite an invalid CSV");
        main.BackCommand.Execute(null);
        main.OpenListCommand.Execute("Conceptos");
        check(main.IsListsVisible && warning == "Concepts.LoadError", "A failed concept load keeps the main view available");
    }
}
