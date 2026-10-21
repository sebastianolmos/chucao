using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using Chucao.Services;

namespace Chucao.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private string? selectedList;
    private string draftListName = "";
    private bool isCreatingList;
    private bool isDeletingList;
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
    }

    public LocalizationService Localization { get; } = new();
    public ObservableCollection<string> Lists { get; }
    public string? SelectedList => selectedList;
    public bool HasSelectedList => selectedList is not null;
    public bool IsListsVisible => !HasSelectedList;
    public bool IsCreatingList => isCreatingList;
    public bool IsDeletingList => isDeletingList;
    public bool IsDialogOpen => isCreatingList || isDeletingList;
    public bool IsContentEnabled => !IsDialogOpen;
    public bool CanCreateList => !string.IsNullOrWhiteSpace(DraftListName) && DraftListName.Length <= 30;

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

    public event Action<string, string?>? WarningRequested;

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
        selectedList = name;
        Notify(nameof(SelectedList));
        Notify(nameof(HasSelectedList));
        Notify(nameof(IsListsVisible));
    }

    private void SetDialog(bool creating, bool deleting)
    {
        isCreatingList = creating;
        isDeletingList = deleting;
        Notify(nameof(IsCreatingList));
        Notify(nameof(IsDeletingList));
        Notify(nameof(IsDialogOpen));
        Notify(nameof(IsContentEnabled));
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
