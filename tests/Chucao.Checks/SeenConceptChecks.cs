using Chucao.Services;
using Chucao.ViewModels;

internal static class SeenConceptChecks
{
    public static void Run(string temporaryDirectory, Action<bool, string> check)
    {
        string directory = Path.Combine(temporaryDirectory, "SeenConceptChecks");
        var files = new ListFileService(directory);
        files.CreateList("Práctica");
        string csvPath = Directory.GetFiles(directory, "*.csv").Single();
        var main = new MainViewModel(files);
        string? warning = null;
        main.WarningRequested += (key, _) => warning = key;
        main.LoadLists();
        main.OpenListCommand.Execute("Práctica");
        check(main.IsConceptListEmpty && !main.AllConceptsSeen && !main.HasSeenConcept,
            "A new list is empty rather than complete and has no last seen concept");

        files.AddConcept("Práctica", "Uno");
        files.AddConcept("Práctica", "Célula, \"unidad\"");
        files.AddConcept("Práctica", "Tres");
        main.OpenListCommand.Execute("Práctica");
        var second = main.UnseenConcepts[1];
        main.MarkConceptAsSeenCommand.Execute(second);
        check(main.UnseenConcepts.Select(entry => entry.Concept).SequenceEqual(new[] { "Uno", "Tres" })
            && main.Concepts.Count == 3 && main.LastSeenConcept == second.Concept,
            "Marking hides only the chosen concept and updates the last seen text");
        check(files.LoadConcepts("Práctica").Single(entry => entry.Concept == second.Concept).SeenOrder == 1,
            "The first seen concept is saved with selection order one");
        main.MarkConceptAsSeenCommand.Execute(second);
        check(files.LoadConcepts("Práctica").Max(entry => entry.SeenOrder) == 1,
            "A repeated command on a hidden row does not advance the order again");
        bool duplicateRejected = false;
        try { files.AddConcept("Práctica", second.Concept); }
        catch (DuplicateConceptException) { duplicateRejected = true; }
        check(duplicateRejected, "Hidden seen concepts still count when checking exact duplicates");

        var reopened = new MainViewModel(new ListFileService(directory));
        reopened.LoadLists();
        reopened.OpenListCommand.Execute("Práctica");
        check(reopened.LastSeenConcept == second.Concept && reopened.UnseenConcepts.Count == 2,
            "A new session recovers hidden concepts and the last seen text");
        main.MarkConceptAsSeenCommand.Execute(main.UnseenConcepts.Last());
        main.MarkConceptAsSeenCommand.Execute(main.UnseenConcepts.Single());
        check(main.AllConceptsSeen && !main.IsConceptListEmpty && main.UnseenConcepts.Count == 0
            && main.LastSeenConcept == "Uno" && files.LoadConcepts("Práctica").Max(entry => entry.SeenOrder) == 3,
            "Finishing all concepts preserves them and reports completion without resetting");
        main.Localization.CurrentLanguage = "es";
        main.Localization.CurrentLanguage = "en";
        check(main.LastSeenConcept == "Uno" && main.AllConceptsSeen,
            "Switching language preserves completion and the last concept text");

        files.AddConcept("Práctica", "Nuevo");
        main.OpenListCommand.Execute("Práctica");
        check(!main.AllConceptsSeen && main.UnseenConcepts.Single().Concept == "Nuevo" && main.LastSeenConcept == "Uno",
            "Adding to a completed list shows the new concept and preserves the last seen one");
        main.DeleteConceptCommand.Execute(main.UnseenConcepts.Single());
        check(main.AllConceptsSeen && files.LoadConcepts("Práctica").Count == 3,
            "Deleting the last pending concept reports completion if seen concepts remain");

        main.ResetListCommand.Execute(null);
        check(main.UnseenConcepts.Count == 3 && !main.HasSeenConcept && !main.AllConceptsSeen
            && files.LoadConcepts("Práctica").All(entry => entry.SeenOrder == 0),
            "Reset restores all seen concepts and clears the last seen information");
        var removed = main.UnseenConcepts[1];
        main.DeleteConceptCommand.Execute(removed);
        main.ResetListCommand.Execute(null);
        check(main.Concepts.Count == 2 && files.LoadConcepts("Práctica").All(entry => entry.Concept != removed.Concept),
            "Deleting removes a row permanently and resetting does not restore it");

        string savedCsv = File.ReadAllText(csvPath);
        using (var locked = File.Open(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var target = main.UnseenConcepts.First();
            main.MarkConceptAsSeenCommand.Execute(target);
            check(warning == "Concepts.MarkSeenError" && main.UnseenConcepts.Count == 2 && !main.HasSeenConcept,
                "A failed mark preserves visibility and the previous last seen information");
            main.DeleteConceptCommand.Execute(target);
            check(warning == "Concepts.DeleteError" && main.UnseenConcepts.Count == 2,
                "A failed deletion preserves the concept on screen");
        }
        check(File.ReadAllText(csvPath) == savedCsv && Directory.GetFiles(directory, "*.tmp").Length == 0,
            "Failed marking and deletion preserve the original CSV and clean up temporary files");

        foreach (var entry in main.UnseenConcepts.ToArray())
            main.DeleteConceptCommand.Execute(entry);
        check(main.IsConceptListEmpty && !main.AllConceptsSeen && !main.HasSeenConcept
            && files.LoadConcepts("Práctica").Count == 0,
            "Deleting every concept leaves an empty list rather than a completed list");

        File.WriteAllText(csvPath, "Concept,SeenOrder\r\nAnterior,2\r\nÚltimo,5\r\nPendiente,0\r\n");
        main.OpenListCommand.Execute("Práctica");
        check(main.LastSeenConcept == "Último", "The largest order identifies the last seen concept even when there are gaps");
        main.MarkConceptAsSeenCommand.Execute(main.UnseenConcepts.Single());
        check(files.LoadConcepts("Práctica").Last().SeenOrder == 6 && main.LastSeenConcept == "Pendiente",
            "Marking uses the next order after the maximum rather than the seen count");
        var latest = files.LoadConcepts("Práctica").MaxBy(entry => entry.SeenOrder)!;
        files.DeleteConcept("Práctica", latest);
        main.OpenListCommand.Execute("Práctica");
        check(main.LastSeenConcept == "Último", "Removing the latest row recovers the most recent remaining seen concept");

        File.WriteAllText(csvPath, "Concept,SeenOrder\r\nIgual,0\r\nIgual,0\r\n");
        main.OpenListCommand.Execute("Práctica");
        main.MarkConceptAsSeenCommand.Execute(main.UnseenConcepts.First());
        check(main.UnseenConcepts.Count == 1 && main.Concepts.Count == 2,
            "Legacy duplicate text marks only one row rather than every match");
        main.DeleteConceptCommand.Execute(main.UnseenConcepts.Single());
        check(files.LoadConcepts("Práctica").Count == 1 && main.LastSeenConcept == "Igual",
            "Deleting a legacy duplicate preserves the separate seen row");

        File.WriteAllText(csvPath, "Concept,SeenOrder\r\nVisto,2147483647\r\nPendiente,0\r\n");
        main.OpenListCommand.Execute("Práctica");
        savedCsv = File.ReadAllText(csvPath);
        main.MarkConceptAsSeenCommand.Execute(main.UnseenConcepts.Single());
        check(warning == "Concepts.MarkSeenError" && main.LastSeenConcept == "Visto"
            && File.ReadAllText(csvPath) == savedCsv,
            "An exhausted order range reports an error instead of overflowing or resetting progress");
    }
}
