using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Chucao.Services;
using Chucao.ViewModels;
using CsvHelper;

namespace Chucao.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;
    private readonly SettingsService settings = new();
    private bool canSaveSettings = true;
    private IInputElement? dialogPreviousFocus;
    private readonly DispatcherTimer additionNoticeTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly TimerSoundPlayer previewSound = new();

    public MainWindow()
    {
        viewModel = new MainViewModel(settings: settings);
        InitializeComponent();
        NativeWindowColors.Attach(this);
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.WarningRequested += ShowListWarning;
        viewModel.SoundPreviewRequested += volume => previewSound.Play(volume);
        previewSound.PlaybackFailed += () => ShowSettingsWarning("Timer.SoundError");
        additionNoticeTimer.Tick += AdditionNoticeTimer_Tick;
        Closed += (_, _) =>
        {
            additionNoticeTimer.Stop();
            previewSound.Stop();
            viewModel.Timer.Pause();
        };
        Loaded += (_, _) => viewModel.LoadLists();
        Loaded += (_, _) => viewModel.LoadTimerSettings();
        try
        {
            viewModel.Localization.CurrentLanguage = settings.LoadLanguage();
        }
        catch (Exception exception) when (IsSettingsError(exception))
        {
            // Preserve unreadable settings instead of replacing them with defaults.
            canSaveSettings = false;
            Loaded += (_, _) => ShowSettingsWarning("Settings.ReadError");
        }
    }

    // Focus belongs to the view; list contents and navigation state belong to the ViewModel.
    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.DraftTimerSoundVolume))
            previewSound.Volume = viewModel.DraftTimerSoundVolume;
        if (e.PropertyName == nameof(MainViewModel.IsShowingSettings) && !viewModel.IsShowingSettings)
            previewSound.Stop();
        if (e.PropertyName == nameof(MainViewModel.IsShowingCredits))
        {
            if (viewModel.IsShowingCredits)
                previewSound.Stop();
            else if (viewModel.IsShowingSettings)
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (viewModel.IsShowingSettings && !viewModel.IsShowingCredits)
                        SettingsContent.FocusCreditsButton();
                }));
        }
        if (e.PropertyName == nameof(MainViewModel.AdditionNoticeCount))
        {
            additionNoticeTimer.Stop();
            if (viewModel.HasAdditionNotice)
                additionNoticeTimer.Start();
        }
        if (e.PropertyName == nameof(MainViewModel.IsDialogOpen))
        {
            if (viewModel.IsDialogOpen)
                dialogPreviousFocus = Keyboard.FocusedElement;
            else
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dialogPreviousFocus is UIElement { IsVisible: true, IsEnabled: true } element)
                        element.Focus();
                    else if (viewModel.IsListDetailVisible)
                        BackButton.Focus();
                    else if (viewModel.IsListsVisible)
                        ListsContent.FocusAddButton();
                }));
        }

        if (e.PropertyName == nameof(MainViewModel.IsTimerVisible) && !viewModel.IsTimerVisible)
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (viewModel.IsListsVisible && !viewModel.IsDialogOpen)
                    ListsContent.FocusTimerButton();
            }));

        if (e.PropertyName == nameof(MainViewModel.HasSelectedList))
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (viewModel.HasSelectedList)
                    BackButton.Focus();
                else
                    ListsContent.FocusAddButton();
            }));
    }

    private void DialogOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            viewModel.CancelDialogCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void LanguageButton_Click(object sender, RoutedEventArgs e)
    {
        var localization = viewModel.Localization;
        localization.CurrentLanguage = localization.CurrentLanguage == "en" ? "es" : "en";
        if (!canSaveSettings)
            return;
        try
        {
            settings.SaveLanguage(localization.CurrentLanguage);
        }
        catch (Exception exception) when (IsSettingsError(exception))
        {
            ShowSettingsWarning("Settings.WriteError");
        }
    }

    private static bool IsSettingsError(Exception exception) =>
        exception is IOException or InvalidDataException or UnauthorizedAccessException or CsvHelperException;

    private void ShowSettingsWarning(string key) => ShowWarning(viewModel.Localization[key]);

    private void ShowListWarning(string key, string? details) => ShowWarning(
        viewModel.Localization[key] + (details is null ? "" : Environment.NewLine + details));

    private void ShowWarning(string message) =>
        new NoticeWindow(viewModel.Localization, message) { Owner = this }.ShowDialog();

    private void AdditionNoticeTimer_Tick(object? sender, EventArgs e)
    {
        additionNoticeTimer.Stop();
        viewModel.ClearAdditionNotice();
    }
}
