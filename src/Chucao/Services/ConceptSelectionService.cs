using Chucao.Models;

namespace Chucao.Services;

public sealed class ConceptSelectionService(ListFileService listFiles, Random? random = null)
{
    private readonly Random selectionRandom = random ?? Random.Shared;

    public (string? SelectedConcept, IReadOnlyList<ConceptEntry> Concepts) SelectRandomConcept(string listName)
    {
        var concepts = listFiles.LoadConcepts(listName);
        var available = concepts.Where(entry => entry.SeenOrder == 0).ToArray();
        if (available.Length == 0)
            return (null, concepts);

        var selected = available[selectionRandom.Next(available.Length)];
        // Save before exposing the result: a failed write must not consume a concept on screen.
        var updated = listFiles.MarkConceptAsSeen(listName, selected);
        return (selected.Concept, updated);
    }
}
