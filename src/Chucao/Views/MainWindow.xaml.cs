using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Chucao.Services;
using Chucao.ViewModels;
using CsvHelper;

namespace Chucao.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel viewModel = new();
    private readonly SettingsService settings = new();
    private bool canSaveSettings = true;
    private IInputElement? dialogPreviousFocus;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.WarningRequested += ShowListWarning;
        Loaded += (_, _) => viewModel.LoadLists();
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
        if (e.PropertyName == nameof(MainViewModel.IsDialogOpen))
        {
            if (viewModel.IsDialogOpen)
                dialogPreviousFocus = Keyboard.FocusedElement;
            else
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (dialogPreviousFocus is UIElement { IsVisible: true, IsEnabled: true } element)
                        element.Focus();
                    else
                        ListsContent.FocusAddButton();
                }));
        }

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

    private void ShowSettingsWarning(string key) => MessageBox.Show(
        this, viewModel.Localization[key], viewModel.Localization["Window.Title"],
        MessageBoxButton.OK, MessageBoxImage.Warning);

    private void ShowListWarning(string key, string? details) => MessageBox.Show(
        this, viewModel.Localization[key] + (details is null ? "" : Environment.NewLine + details),
        viewModel.Localization["Window.Title"], MessageBoxButton.OK, MessageBoxImage.Warning);
}
