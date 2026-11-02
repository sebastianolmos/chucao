using Chucao.Services;
using Chucao.ViewModels;

internal static class RandomConceptChecks
{
    public static void Run(string temporaryDirectory, Action<bool, string> check)
    {
        string directory = Path.Combine(temporaryDirectory, "RandomConceptChecks");
        var files = new ListFileService(directory);
        files.CreateList("Sorteo");
        string csvPath = Directory.GetFiles(directory, "*.csv").Single();
        var random = new LastCandidateRandom();
        var main = new MainViewModel(files, random);
        string? warning = null;
        main.WarningRequested += (key, _) => warning = key;
        main.SelectRandomConceptCommand.Execute(null);
        main.LoadLists();
        main.OpenListCommand.Execute("Sorteo");
        main.SelectRandomConceptCommand.Execute(null);
        check(!main.CanSelectRandomConcept && !main.IsDialogOpen && random.Calls == 0,
            "Selecting without a list or from an empty list does not draw or open a result");

        files.AddConcepts("Sorteo", ["Uno", "Dos", "Tres"]);
        files.MarkConceptAsSeen("Sorteo", files.LoadConcepts("Sorteo").First());
        main.OpenListCommand.Execute("Sorteo");
        check(main.CanSelectRandomConcept, "A list with unused concepts enables random selection");
        main.SelectRandomConceptCommand.Execute(null);
        check(main.IsShowingSelectedConcept && main.SelectedConcept == "Tres" && main.LastSeenConcept == "Tres"
            && main.UnseenConcepts.Single().Concept == "Dos", "A draw shows only an available concept and hides it from the pending list");
        check(files.LoadConcepts("Sorteo").Last().SeenOrder == 2,
            "The selected concept is saved as the last seen before returning the result");
        main.SelectRandomConceptCommand.Execute(null);
        check(random.Calls == 1 && main.SelectedConcept == "Tres", "An open result prevents another draw");
        main.Localization.CurrentLanguage = "es";
        main.Localization.CurrentLanguage = "en";
        check(main.IsShowingSelectedConcept && main.SelectedConcept == "Tres", "Changing language preserves the selected concept");
        main.CancelDialogCommand.Execute(null);
        check(!main.IsDialogOpen && main.SelectedConcept is null && main.LastSeenConcept == "Tres"
            && main.CanSelectRandomConcept, "Returning closes the result while preserving its seen state");

        var reopened = new MainViewModel(new ListFileService(directory), new LastCandidateRandom());
        reopened.LoadLists();
        reopened.OpenListCommand.Execute("Sorteo");
        check(reopened.LastSeenConcept == "Tres" && reopened.UnseenConcepts.Single().Concept == "Dos",
            "Reopening recovers the last draw and excludes it from the available concepts");
        reopened.SelectRandomConceptCommand.Execute(null);
        check(reopened.SelectedConcept == "Dos" && reopened.AllConceptsSeen
            && files.LoadConcepts("Sorteo").Single(entry => entry.Concept == "Dos").SeenOrder == 3,
            "The last available concept completes the round without repeating seen concepts");
        reopened.CancelDialogCommand.Execute(null);
        string savedCsv = File.ReadAllText(csvPath);
        reopened.SelectRandomConceptCommand.Execute(null);
        check(!reopened.CanSelectRandomConcept && !reopened.IsDialogOpen && File.ReadAllText(csvPath) == savedCsv,
            "An exhausted list cannot draw again or reset itself");
        reopened.ResetListCommand.Execute(null);
        reopened.SelectRandomConceptCommand.Execute(null);
        check(reopened.SelectedConcept == "Tres" && reopened.LastSeenConcept == "Tres"
            && files.LoadConcepts("Sorteo").Last().SeenOrder == 1,
            "An explicit reset makes previously selected concepts available for a new round");
        reopened.CancelDialogCommand.Execute(null);

        main.OpenListCommand.Execute("Sorteo");
        main.ResetListCommand.Execute(null);
        savedCsv = File.ReadAllText(csvPath);
        using (var locked = File.Open(csvPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            main.SelectRandomConceptCommand.Execute(null);
            check(warning == "Concepts.RandomError" && !main.IsDialogOpen && main.SelectedConcept is null
                && !main.HasSeenConcept && main.UnseenConcepts.Count == 3,
                "A failed save reports an error without presenting or consuming a selected concept");
        }
        check(File.ReadAllText(csvPath) == savedCsv && Directory.GetFiles(directory, "*.tmp").Length == 0,
            "A failed random draw preserves the original CSV and removes temporary files");

        var selector = new ConceptSelectionService(files, new LastCandidateRandom());
        var drawn = new HashSet<string>();
        for (int i = 0; i < 3; i++)
        {
            var result = selector.SelectRandomConcept("Sorteo");
            check(result.SelectedConcept is not null && drawn.Add(result.SelectedConcept)
                && result.Concepts.Max(entry => entry.SeenOrder) == i + 1,
                "Drawing through a round returns a new concept and persists increasing seen order");
        }
        var exhausted = selector.SelectRandomConcept("Sorteo");
        check(exhausted.SelectedConcept is null && exhausted.Concepts.All(entry => entry.SeenOrder > 0),
            "The selection service reports no available concepts without resetting progress");

        main.OpenListCommand.Execute("Sorteo");
        main.ResetListCommand.Execute(null);
        foreach (var entry in files.LoadConcepts("Sorteo").ToArray())
            files.MarkConceptAsSeen("Sorteo", entry);
        main.SelectRandomConceptCommand.Execute(null);
        check(!main.IsDialogOpen && main.AllConceptsSeen && !main.CanSelectRandomConcept,
            "A draw refreshes stale availability if the CSV was completed elsewhere");
    }

    // Deterministic choice for checking eligible candidates and persistence, not random distribution.
    private sealed class LastCandidateRandom : Random
    {
        public int Calls { get; private set; }

        public override int Next(int maxValue)
        {
            if (maxValue <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            Calls++;
            return maxValue - 1;
        }
    }
}
