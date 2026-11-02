using System.Globalization;
using Chucao.Services;
using Chucao.ViewModels;

internal static class ConceptSetChecks
{
    public static void Run(string temporaryDirectory, Action<bool, string> check)
    {
        string directory = Path.Combine(temporaryDirectory, "ConceptSetChecks");
        var files = new ListFileService(directory);
        files.CreateList("Conjunto");
        string path = Directory.GetFiles(directory, "*.csv").Single();
        File.WriteAllText(path, "Concept,SeenOrder\r\nVisto,5\r\nExistente,0\r\n");
        var main = new MainViewModel(files);
        string? warning = null;
        main.WarningRequested += (key, _) => warning = key;
        main.AddConceptSetCommand.Execute(null);
        check(!main.IsDialogOpen, "A concept set cannot be added without selecting a list");
        main.LoadLists();
        main.OpenListCommand.Execute("Conjunto");

        void Begin(string text)
        {
            main.AddConceptSetCommand.Execute(null);
            main.DraftConceptSet = text;
        }

        Begin(" - Matematicas \r\n- Biologia\n- Matematicas\r- Visto\n- Existente\n- "
            + new string('a', 31) + "\n• Viñeta\nSin guion\n-   \n\n- Relación causa-efecto\n- COmputacion");
        main.ConfirmAddConceptSetCommand.Execute(null);
        string[] added = ["Matematicas", "Biologia", "Relación causa-efecto", "COmputacion"];
        check(!main.IsDialogOpen && main.Concepts.Skip(2).Select(entry => entry.Concept).SequenceEqual(added),
            "Mixed line endings, outer spaces and ASCII dashes produce concepts in input order");
        check(main.Concepts.Take(2).Select(entry => entry.SeenOrder).SequenceEqual(new[] { 5, 0 })
            && main.Concepts.Skip(2).All(entry => entry.SeenOrder == 0) && main.LastSeenConcept == "Visto",
            "A set preserves existing progress and adds every new concept with state zero");
        check(main.AdditionNoticeCount == 4 && main.AdditionNoticeText == string.Format(CultureInfo.CurrentCulture,
            main.Localization["Concepts.AddedMany"], 4), "The notice reports only the number successfully added");
        main.Localization.CurrentLanguage = "es";
        check(main.AdditionNoticeText == string.Format(CultureInfo.CurrentCulture,
            main.Localization["Concepts.AddedMany"], 4), "The count notice follows the selected language");
        main.ClearAdditionNotice();
        check(!main.HasAdditionNotice && main.AdditionNoticeText == "", "Expiring a notice clears its text and visibility");
        var reopened = new MainViewModel(new ListFileService(directory));
        reopened.LoadLists();
        reopened.OpenListCommand.Execute("Conjunto");
        check(reopened.Concepts.SequenceEqual(main.Concepts), "Every accepted concept survives reopening the list");

        string savedCsv = File.ReadAllText(path);
        Begin("- " + new string('b', 31) + "\n• No\n- ");
        main.ConfirmAddConceptSetCommand.Execute(null);
        check(!main.IsDialogOpen && main.AdditionNoticeCount == 0 && File.ReadAllText(path) == savedCsv,
            "An entirely invalid block reports zero additions without changing the CSV");
        Begin("- Corregido");
        main.CancelDialogCommand.Execute(null);
        check(!main.IsDialogOpen && File.ReadAllText(path) == savedCsv, "Canceling a pasted set does not write concepts");

        Begin("- Visto\n- Visto\n- Existente");
        using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            main.ConfirmAddConceptSetCommand.Execute(null);
            check(!main.IsDialogOpen && main.AdditionNoticeCount == 0 && warning is null,
                "An entirely duplicate block does not attempt to replace a locked CSV");
        }
        check(File.ReadAllText(path) == savedCsv, "Existing seen concepts and duplicates remain unchanged");
        main.CancelDialogCommand.Execute(null);

        main.ClearAdditionNotice();
        Begin("- Uno nuevo\n- Dos nuevos");
        string draft = main.DraftConceptSet;
        int previousCount = main.Concepts.Count;
        using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            main.ConfirmAddConceptSetCommand.Execute(null);
            check(main.IsAddingConceptSet && main.DraftConceptSet == draft
                && main.Concepts.Count == previousCount && warning == "Concepts.SetSaveError" && !main.HasAdditionNotice,
                "A failed batch save preserves the complete draft and displayed list");
        }
        check(File.ReadAllText(path) == savedCsv && Directory.GetFiles(directory, "*.tmp").Length == 0,
            "A failed batch leaves no partial additions or temporary files");
        main.ConfirmAddConceptSetCommand.Execute(null);
        check(!main.IsDialogOpen && files.LoadConcepts("Conjunto").Count == previousCount + 2 && main.AdditionNoticeCount == 2,
            "Retrying the retained draft saves the entire set once");

        Begin("- visto\n- " + new string('ñ', 30) + "\n- Célula, \"unidad\"");
        main.ConfirmAddConceptSetCommand.Execute(null);
        check(main.Concepts.Any(entry => entry.Concept == "visto")
            && main.Concepts.Any(entry => entry.Concept == new string('ñ', 30))
            && files.LoadConcepts("Conjunto").Last().Concept == "Célula, \"unidad\"",
            "Sets preserve case, support 30 characters and round-trip CSV punctuation");

        savedCsv = File.ReadAllText(path);
        bool rejected = false;
        try { files.AddConcepts("Conjunto", ["Candidate", new string('x', 31)]); }
        catch (ArgumentException) { rejected = true; }
        check(rejected && File.ReadAllText(path) == savedCsv,
            "Invalid service input cannot save a partial batch");

        main.AddConceptSetCommand.Execute(null);
        check(main.DraftConceptSet == "", "Reopening the form starts with an empty draft");
        main.DraftConceptSet = "   ";
        main.ConfirmAddConceptSetCommand.Execute(null);
        check(!main.CanAddConceptSet && main.IsAddingConceptSet && File.ReadAllText(path) == savedCsv,
            "A whitespace-only block cannot be confirmed");
        main.CancelDialogCommand.Execute(null);
        main.BackCommand.Execute(null);
        check(!main.HasAdditionNotice, "Leaving the list removes its previous addition notice");
    }
}
