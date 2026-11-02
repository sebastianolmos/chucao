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
    private int? additionNoticeCount;
    private readonly ListFileService listFiles;
    private readonly ConceptSelectionService conceptSelection;
    private bool isShowingSelectedConcept;
    private string? selectedConcept;
    private readonly SettingsService settings;
    private bool isShowingSettings;
    private bool isTimerVisible;
    private bool timerReturnsToConcept;
    private int configuredTimerMinutes = 1;
    private int draftTimerMinutes = 1;

    public MainViewModel(ListFileService? listFiles = null, Random? random = null,
        SettingsService? settings = null, TimeProvider? timeProvider = null)
    {
        this.listFiles = listFiles ?? new ListFileService();
        this.settings = settings ?? new SettingsService();
        Timer = new TimerViewModel(Localization, timeProvider);
        conceptSelection = new ConceptSelectionService(this.listFiles, random);
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
            SetDialog(creating: false, deleting: false, addingSet: true);
        });
        ConfirmAddConceptSetCommand = new RelayCommand(_ => ConfirmAddConceptSet());
        SelectRandomConceptCommand = new RelayCommand(_ => SelectRandomConcept());
        OpenSettingsCommand = new RelayCommand(_ =>
        {
            if (!IsListsVisible || IsDialogOpen)
                return;
            DraftTimerMinutes = configuredTimerMinutes;
            SetDialog(creating: false, deleting: false, showingSettings: true);
        });
        SaveSettingsCommand = new RelayCommand(_ => SaveTimerSettings());
        OpenTimerCommand = new RelayCommand(_ => OpenTimer(fromConcept: false));
        OpenConceptTimerCommand = new RelayCommand(_ => OpenTimer(fromConcept: true));
        ReturnFromTimerCommand = new RelayCommand(_ => ReturnFromTimer());
        Localization.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(Localization.CurrentLanguage))
                Notify(nameof(AdditionNoticeText));
        };
    }

    public LocalizationService Localization { get; } = new();
    public TimerViewModel Timer { get; }
    public IEnumerable<int> TimerMinuteOptions { get; } = Enumerable.Range(1, 59);
    public int ConfiguredTimerMinutes => configuredTimerMinutes;
    public int DraftTimerMinutes
    {
        get => draftTimerMinutes;
        set
        {
            draftTimerMinutes = value;
            Notify(nameof(DraftTimerMinutes));
            Notify(nameof(CanSaveTimerSettings));
        }
    }
    public bool CanSaveTimerSettings => DraftTimerMinutes is >= 1 and <= 59;
    public bool IsShowingSettings => isShowingSettings;
    public bool IsTimerVisible => isTimerVisible;
    public bool IsListDetailVisible => HasSelectedList && !IsTimerVisible;
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
    public bool IsListsVisible => !HasSelectedList && !IsTimerVisible;
    public bool IsCreatingList => isCreatingList;
    public bool IsDeletingList => isDeletingList;
    public bool IsCreatingConcept => isCreatingConcept;
    public bool IsAddingConceptSet => isAddingConceptSet;
    public bool IsShowingSelectedConcept => isShowingSelectedConcept;
    public string? SelectedConcept => selectedConcept;
    public bool CanSelectRandomConcept => HasSelectedList && UnseenConcepts.Count > 0 && !IsDialogOpen && !IsTimerVisible;
    public bool IsDialogOpen => isCreatingList || isDeletingList || isCreatingConcept || isAddingConceptSet || isShowingSelectedConcept || isShowingSettings;
    public bool IsContentEnabled => !IsDialogOpen;
    public bool CanCreateList => !string.IsNullOrWhiteSpace(DraftListName) && DraftListName.Length <= 30;
    public bool CanAddConcept => !string.IsNullOrWhiteSpace(DraftConceptText) && DraftConceptText.Length <= 30;
    public bool CanAddConceptSet => !string.IsNullOrWhiteSpace(DraftConceptSet);
    public int? AdditionNoticeCount => additionNoticeCount;
    public bool HasAdditionNotice => additionNoticeCount.HasValue;
    public string AdditionNoticeText
    {
        get
        {
            if (additionNoticeCount is not { } count)
                return "";
            return count == 1 ? Localization["Concepts.AddedOne"]
                : string.Format(CultureInfo.CurrentCulture, Localization["Concepts.AddedMany"], count);
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
    public ICommand SelectRandomConceptCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand SaveSettingsCommand { get; }
    public ICommand OpenTimerCommand { get; }
    public ICommand OpenConceptTimerCommand { get; }
    public ICommand ReturnFromTimerCommand { get; }

    public event Action<string, string?>? WarningRequested;

    public void LoadTimerSettings()
    {
        try
        {
            configuredTimerMinutes = settings.LoadTimerMinutes();
            Notify(nameof(ConfiguredTimerMinutes));
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            configuredTimerMinutes = 1;
            Notify(nameof(ConfiguredTimerMinutes));
            WarningRequested?.Invoke("Timer.SettingsReadError", null);
        }
    }

    private void SaveTimerSettings()
    {
        if (!IsShowingSettings || !CanSaveTimerSettings)
            return;
        try
        {
            settings.SaveTimerMinutes(DraftTimerMinutes);
            configuredTimerMinutes = DraftTimerMinutes;
            Notify(nameof(ConfiguredTimerMinutes));
            SetDialog(creating: false, deleting: false);
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Timer.SettingsWriteError", null);
        }
    }

    private void OpenTimer(bool fromConcept)
    {
        if (IsTimerVisible || (fromConcept && !IsShowingSelectedConcept)
            || (!fromConcept && (!IsListsVisible || IsDialogOpen)))
            return;
        string? concept = fromConcept ? selectedConcept : null;
        timerReturnsToConcept = fromConcept;
        SetDialog(creating: false, deleting: false);
        ClearAdditionNotice();
        Timer.Prepare(configuredTimerMinutes, concept);
        SetTimerVisible(true);
    }

    private void ReturnFromTimer()
    {
        if (!IsTimerVisible)
            return;
        Timer.Pause();
        SetTimerVisible(false);
        if (timerReturnsToConcept)
        {
            selectedConcept = Timer.Concept;
            Notify(nameof(SelectedConcept));
            SetDialog(creating: false, deleting: false, showingSelectedConcept: true);
        }
    }

    private void SetTimerVisible(bool visible)
    {
        isTimerVisible = visible;
        Notify(nameof(IsTimerVisible));
        Notify(nameof(IsListsVisible));
        Notify(nameof(IsListDetailVisible));
        Notify(nameof(CanSelectRandomConcept));
    }

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
        ClearAdditionNotice();
        selectedList = name;
        Notify(nameof(SelectedList));
        Notify(nameof(HasSelectedList));
        Notify(nameof(IsListDetailVisible));
        Notify(nameof(IsListsVisible));
        Notify(nameof(CanSelectRandomConcept));
    }

    private void SetDialog(bool creating, bool deleting, bool addingConcept = false, bool addingSet = false,
        bool showingSelectedConcept = false, bool showingSettings = false)
    {
        isCreatingList = creating;
        isDeletingList = deleting;
        isCreatingConcept = addingConcept;
        isAddingConceptSet = addingSet;
        isShowingSelectedConcept = showingSelectedConcept;
        isShowingSettings = showingSettings;
        if (!showingSelectedConcept)
        {
            selectedConcept = null;
            Notify(nameof(SelectedConcept));
        }
        Notify(nameof(IsCreatingList));
        Notify(nameof(IsDeletingList));
        Notify(nameof(IsCreatingConcept));
        Notify(nameof(IsAddingConceptSet));
        Notify(nameof(IsShowingSelectedConcept));
        Notify(nameof(IsShowingSettings));
        Notify(nameof(IsDialogOpen));
        Notify(nameof(IsContentEnabled));
        Notify(nameof(CanSelectRandomConcept));
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
        Notify(nameof(CanSelectRandomConcept));
    }

    private void SelectRandomConcept()
    {
        if (!CanSelectRandomConcept || selectedList is null)
            return;
        try
        {
            var result = conceptSelection.SelectRandomConcept(selectedList);
            ReplaceConcepts(result.Concepts);
            if (result.SelectedConcept is null)
                return;

            ClearAdditionNotice();
            selectedConcept = result.SelectedConcept;
            Notify(nameof(SelectedConcept));
            SetDialog(creating: false, deleting: false, showingSelectedConcept: true);
        }
        catch (Exception exception) when (ListFileService.IsFileError(exception))
        {
            WarningRequested?.Invoke("Concepts.RandomError", null);
        }
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
        ShowAdditionNotice(1);
    }

    public void ClearAdditionNotice()
    {
        additionNoticeCount = null;
        NotifyAdditionNotice();
    }

    private void ShowAdditionNotice(int count)
    {
        additionNoticeCount = count;
        // Notify even when the count repeats so the view restarts its ten-second timer.
        NotifyAdditionNotice();
    }

    private void NotifyAdditionNotice()
    {
        Notify(nameof(AdditionNoticeCount));
        Notify(nameof(HasAdditionNotice));
        Notify(nameof(AdditionNoticeText));
    }

    private void ConfirmAddConceptSet()
    {
        if (!IsAddingConceptSet || !CanAddConceptSet || selectedList is null)
            return;

        var candidates = new List<string>();
        using var reader = new StringReader(DraftConceptSet);
        while (reader.ReadLine() is { } line)
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith('-'))
                continue;
            // Remove only the first ASCII dash; preserve internal dashes and casing.
            string concept = trimmed[1..].Trim();
            if (concept.Length is > 0 and <= 30)
                candidates.Add(concept);
        }

        if (candidates.Count == 0)
        {
            SetDialog(creating: false, deleting: false);
            ShowAdditionNotice(0);
            return;
        }
        try
        {
            var result = listFiles.AddConcepts(selectedList, candidates);
            ReplaceConcepts(result.Concepts);
            SetDialog(creating: false, deleting: false);
            ShowAdditionNotice(result.Added);
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
