using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Input;
using Chucao.Services;
using Chucao.Models;

namespace Chucao.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string? selectedList;
    private string draftListName = "";
    private bool isCreatingList;
    private bool isDeletingList;
    private bool isCreatingConcept;
    private string draftConceptText = "";
    private bool isAddingConceptSet;
    private string draftConceptSet = "";
    private (int Added, int Duplicates, int TooLong, int Invalid, int Empty)? conceptSetSummary;
    private readonly ListFileService listFiles;

    public MainViewModel(ListFileService? listFiles = null)
    {
        this.listFiles = listFiles ?? new ListFileService();
        Lists = new ObservableCollection<string>();
        OpenListCommand = new RelayCommand(parameter =>
        {
            if (parameter is string name && Lists.Contains(name))
                SelectList(name);
        });
        BackCommand = new RelayCommand(_ => SelectList(null));
        CreateListCommand = new RelayCommand(_ =>
        {
            DraftListName = "";
            SetDialog(creating: true, deleting: false);
        });
        ConfirmCreateCommand = new RelayCommand(_ => ConfirmCreate());
        DeleteListCommand = new RelayCommand(_ =>
        {
            if (HasSelectedList)
                SetDialog(creating: false, deleting: true);
        });
        ConfirmDeleteCommand = new RelayCommand(_ => ConfirmDelete());
        CancelDialogCommand = new RelayCommand(_ => SetDialog(creating: false, deleting: false));
        AddConceptCommand = new RelayCommand(_ =>
        {
            if (!HasSelectedList)
                return;
            DraftConceptText = "";
            SetDialog(creating: false, deleting: false, addingConcept: true);
        });
        ConfirmAddConceptCommand = new RelayCommand(_ => ConfirmAddConcept());
        ResetListCommand = new RelayCommand(_ => ResetSelectedList());
        MarkConceptAsSeenCommand = new RelayCommand(parameter =>
        {
            if (parameter is ConceptEntry concept)
                MarkConceptAsSeen(concept);
        });
        DeleteConceptCommand = new RelayCommand(parameter =>
        {
            if (parameter is ConceptEntry concept)
                DeleteConcept(concept);
        });
        AddConceptSetCommand = new RelayCommand(_ =>
        {
            if (!HasSelectedList || IsDialogOpen)
                return;
            DraftConceptSet = "";
            SetConceptSetSummary(null);
            SetDialog(creating: false, deleting: false, addingSet: true);
        });
        ConfirmAddConceptSetCommand = new RelayCommand(_ => ConfirmAddConceptSet());
        Localization.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Localization.CurrentLanguage))
                Notify(nameof(ConceptSetSummary));
        };
    }

    public LocalizationService Localization { get; } = new();
    public ObservableCollection<string> Lists { get; }
    public ObservableCollection<ConceptEntry> Concepts { get; } = new();
    public ObservableCollection<ConceptEntry> UnseenConcepts { get; } = new();
    public string? LastSeenConcept => Concepts.Where(entry => entry.SeenOrder > 0)
        .MaxBy(entry => entry.SeenOrder)?.Concept;
    public bool HasSeenConcept => LastSeenConcept is not null;
    public bool AllConceptsSeen => Concepts.Count > 0 && UnseenConcepts.Count == 0;
    public bool IsConceptListEmpty => Concepts.Count == 0;
    public string? SelectedList => selectedList;
    public bool HasSelectedList => selectedList is not null;
    public bool IsListsVisible => !HasSelectedList;
    public bool IsCreatingList => isCreatingList;
    public bool IsDeletingList => isDeletingList;
    public bool IsCreatingConcept => isCreatingConcept;
    public bool IsAddingConceptSet => isAddingConceptSet;
    public bool IsDialogOpen => isCreatingList || isDeletingList || isCreatingConcept || isAddingConceptSet;
    public bool IsContentEnabled => !IsDialogOpen;
    public bool CanCreateList => !string.IsNullOrWhiteSpace(DraftListName) && DraftListName.Length <= 30;
    public bool CanAddConcept => !string.IsNullOrWhiteSpace(DraftConceptText) && DraftConceptText.Length <= 30;
    public bool CanAddConceptSet => !string.IsNullOrWhiteSpace(DraftConceptSet);
    public bool HasConceptSetSummary => conceptSetSummary.HasValue;
    public string ConceptSetSummary
    {
        get
        {
            if (conceptSetSummary is not { } summary)
                return "";
            string text = string.Format(CultureInfo.CurrentCulture, Localization["Concepts.SetSummary"],
                summary.Added, summary.Duplicates, summary.TooLong, summary.Invalid, summary.Empty);
            return summary.Added == 0 ? Localization["Concepts.SetNoValid"] + Environment.NewLine + text : text;
        }
    }

    public string DraftConceptSet
    {
        get => draftConceptSet;
        set
        {
            if (draftConceptSet == value)
                return;
            draftConceptSet = value;
            SetConceptSetSummary(null);
            Notify(nameof(DraftConceptSet));
            Notify(nameof(CanAddConceptSet));
        }
    }

    public string DraftConceptText
    {
        get => draftConceptText;
        set
        {
            if (draftConceptText == value)
                return;
            draftConceptText = value;
            Notify(nameof(DraftConceptText));
            Notify(nameof(CanAddConcept));
        }
    }

    public string DraftListName
    {
        get => draftListName;
        set
        {
            if (draftListName == value)
                return;
            draftListName = value;
            Notify(nameof(DraftListName));
            Notify(nameof(CanCreateList));
        }
    }

    public ICommand OpenListCommand { get; }
    public ICommand BackCommand { get; }
    public ICommand CreateListCommand { get; }
    public ICommand ConfirmCreateCommand { get; }
    public ICommand DeleteListCommand { get; }
    public ICommand ConfirmDeleteCommand { get; }
    public ICommand CancelDialogCommand { get; }
    public ICommand AddConceptCommand { get; }
    public ICommand ConfirmAddConceptCommand { get; }
    public ICommand ResetListCommand { get; }
    public ICommand MarkConceptAsSeenCommand { get; }
    public ICommand DeleteConceptCommand { get; }
    public ICommand AddConceptSetCommand { get; }
    public ICommand ConfirmAddConceptSetCommand { get; }

    public event Action<string, string?>? WarningRequested;
    public event Action<string>? InformationRequested;

    public void LoadLists()
    {
        try
        {
            var loaded = listFiles.LoadLists();
            Lists.Clear();
            foreach (string name in loaded.Names)
                Lists.Add(name);
            if (loaded.SkippedFiles.Count > 0)
                WarningRequested?.Invoke("Lists.InvalidFilesWarning", string.Join(Environment.NewLine, loaded.SkippedFiles));
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Lists.LoadError", null);
        }
    }

    private void SelectList(string? name)
    {
        if (name is not null)
        {
            try
            {
                ReplaceConcepts(listFiles.LoadConcepts(name));
            }
            catch (Exception exception) when (ListFileService.IsFileError(exception))
            {
                WarningRequested?.Invoke("Concepts.LoadError", null);
                return;
            }
        }
        else
        {
            ReplaceConcepts(Array.Empty<ConceptEntry>());
        }
        selectedList = name;
        Notify(nameof(SelectedList));
        Notify(nameof(HasSelectedList));
        Notify(nameof(IsListsVisible));
    }

    private void SetDialog(bool creating, bool deleting, bool addingConcept = false, bool addingSet = false)
    {
        isCreatingList = creating;
        isDeletingList = deleting;
        isCreatingConcept = addingConcept;
        isAddingConceptSet = addingSet;
        Notify(nameof(IsCreatingList));
        Notify(nameof(IsDeletingList));
        Notify(nameof(IsCreatingConcept));
        Notify(nameof(IsAddingConceptSet));
        Notify(nameof(IsDialogOpen));
        Notify(nameof(IsContentEnabled));
    }

    private void ReplaceConcepts(IReadOnlyList<ConceptEntry> concepts)
    {
        Concepts.Clear();
        UnseenConcepts.Clear();
        foreach (var entry in concepts)
        {
            Concepts.Add(entry);
            if (entry.SeenOrder == 0)
                UnseenConcepts.Add(entry);
        }
        Notify(nameof(LastSeenConcept));
        Notify(nameof(HasSeenConcept));
        Notify(nameof(AllConceptsSeen));
        Notify(nameof(IsConceptListEmpty));
    }

    private void MarkConceptAsSeen(ConceptEntry concept)
    {
        if (selectedList is null || IsDialogOpen || !UnseenConcepts.Contains(concept))
            return;
        try
        {
            ReplaceConcepts(listFiles.MarkConceptAsSeen(selectedList, concept));
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Concepts.MarkSeenError", null);
        }
    }

    private void DeleteConcept(ConceptEntry concept)
    {
        if (selectedList is null || IsDialogOpen || !UnseenConcepts.Contains(concept))
            return;
        try
        {
            ReplaceConcepts(listFiles.DeleteConcept(selectedList, concept));
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Concepts.DeleteError", null);
        }
    }

    private void ConfirmAddConcept()
    {
        if (!IsCreatingConcept || !CanAddConcept || selectedList is null)
            return;
        try
        {
            ReplaceConcepts(listFiles.AddConcept(selectedList, DraftConceptText));
        }
        catch (DuplicateConceptException)
        {
            WarningRequested?.Invoke("Concepts.Duplicate", null);
            return;
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Concepts.SaveError", null);
            return;
        }
        SetDialog(creating: false, deleting: false);
    }

    private void SetConceptSetSummary((int Added, int Duplicates, int TooLong, int Invalid, int Empty)? summary)
    {
        conceptSetSummary = summary;
        Notify(nameof(ConceptSetSummary));
        Notify(nameof(HasConceptSetSummary));
    }

    private void ConfirmAddConceptSet()
    {
        if (!IsAddingConceptSet || !CanAddConceptSet || selectedList is null)
            return;

        SetConceptSetSummary(null);
        var candidates = new List<string>();
        int tooLong = 0;
        int invalid = 0;
        int empty = 0;
        using var reader = new StringReader(DraftConceptSet);
        while (reader.ReadLine() is { } line)
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0)
                empty++;
            else if (!trimmed.StartsWith('-'))
                invalid++;
            else
            {
                // Remove only the first ASCII dash; preserve internal dashes and casing.
                string concept = trimmed[1..].Trim();
                if (concept.Length == 0)
                    empty++;
                else if (concept.Length > 30)
                    tooLong++;
                else
                    candidates.Add(concept);
            }
        }

        if (candidates.Count == 0)
        {
            SetConceptSetSummary((0, 0, tooLong, invalid, empty));
            return;
        }
        try
        {
            var result = listFiles.AddConcepts(selectedList, candidates);
            ReplaceConcepts(result.Concepts);
            SetConceptSetSummary((result.Added, result.Duplicates, tooLong, invalid, empty));
            if (result.Added > 0)
            {
                SetDialog(creating: false, deleting: false);
                InformationRequested?.Invoke(ConceptSetSummary);
            }
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Concepts.SetSaveError", null);
        }
    }

    private void ResetSelectedList()
    {
        if (selectedList is null || IsDialogOpen)
            return;
        try
        {
            ReplaceConcepts(listFiles.ResetList(selectedList));
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Concepts.ResetError", null);
        }
    }

    private void ConfirmCreate()
    {
        if (!IsCreatingList || !CanCreateList)
            return;
        string name = DraftListName.Trim();
        try
        {
            listFiles.CreateList(name);
        }
        catch (DuplicateListException)
        {
            WarningRequested?.Invoke("Lists.DuplicateName", null);
            return;
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Lists.CreateError", null);
            return;
        }
        Lists.Add(name);
        SetDialog(creating: false, deleting: false);
    }

    private void ConfirmDelete()
    {
        if (!IsDeletingList || selectedList is null)
            return;
        try
        {
            listFiles.DeleteList(selectedList);
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Lists.DeleteError", null);
            return;
        }
        Lists.Remove(selectedList);
        SetDialog(creating: false, deleting: false);
        SelectList(null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
