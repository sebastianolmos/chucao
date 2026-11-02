using Chucao.Services;
using Chucao.ViewModels;

internal static class TimerChecks
{
    public static void Run(string temporaryDirectory, Action<bool, string> check)
    {
        string directory = Path.Combine(temporaryDirectory, "TimerChecks");
        Directory.CreateDirectory(directory);
        string settingsPath = Path.Combine(directory, "settings.csv");
        var settings = new SettingsService(settingsPath);
        check(settings.LoadTimerMinutes() == 1 && !File.Exists(settingsPath), "A new timer defaults to one minute without writing settings");
        string legacy = "Key,Value\nLanguage,es\nNote,Keep\n";
        File.WriteAllText(settingsPath, legacy);
        check(settings.LoadTimerMinutes() == 1 && File.ReadAllText(settingsPath) == legacy,
            "Existing language-only settings use one minute without automatic migration");
        settings.SaveTimerMinutes(59);
        var independentSettings = new SettingsService(settingsPath);
        independentSettings.SaveLanguage("en");
        check(settings.LoadTimerMinutes() == 59 && settings.LoadLanguage() == "en"
            && File.ReadAllText(settingsPath).Contains("Keep"), "Duration and language saves preserve each other and unrelated settings");
        foreach (int invalid in new[] { 0, 60 })
        {
            bool rejected = false;
            try { settings.SaveTimerMinutes(invalid); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            check(rejected && settings.LoadTimerMinutes() == 59, "An out-of-range duration cannot replace the saved preference");
        }

        var clock = new ManualClock();
        var localization = new LocalizationService();
        var timer = new TimerViewModel(localization, clock);
        timer.Prepare(1, "Concepto");
        check(timer.DisplayTime == "01:00" && !timer.IsRunning && timer.Concept == "Concepto",
            "Preparing a timer displays the configured duration without starting");
        timer.ToggleCommand.Execute(null);
        clock.Advance(TimeSpan.FromSeconds(10.2));
        timer.Update();
        check(timer.DisplayTime == "00:50" && timer.IsRunning, "The displayed countdown uses elapsed time and rounds partial seconds up");
        clock.Advance(TimeSpan.FromSeconds(9.8));
        timer.ToggleCommand.Execute(null);
        clock.Advance(TimeSpan.FromMinutes(2));
        timer.Update();
        check(!timer.IsRunning && timer.DisplayTime == "00:40", "Pausing captures elapsed time and excludes time spent paused");
        timer.ToggleCommand.Execute(null);
        clock.Advance(TimeSpan.FromSeconds(39.2));
        timer.Update();
        check(timer.DisplayTime == "00:01", "Resuming continues from the paused remainder");
        clock.Advance(TimeSpan.FromSeconds(3));
        timer.Update();
        check(timer.IsFinished && !timer.IsRunning && timer.DisplayTime == "00:00"
            && timer.Remaining == TimeSpan.Zero, "An overdue update finishes at zero without negative time");
        timer.ToggleCommand.Execute(null);
        check(timer.IsRunning && timer.DisplayTime == "01:00", "A finished timer can start a new countdown with the configured duration");
        localization.CurrentLanguage = "es";
        check(timer.ActionText == localization["Timer.Pause"] && timer.Concept == "Concepto" && timer.IsRunning,
            "Changing language updates timer controls without resetting its state or concept");
        timer.Pause();

        settings.SaveTimerMinutes(2);
        var files = new ListFileService(Path.Combine(directory, "Lists"));
        var main = new MainViewModel(files, settings: settings, timeProvider: clock);
        main.LoadTimerSettings();
        main.OpenTimerCommand.Execute(null);
        check(main.IsTimerVisible && !main.IsListsVisible && main.Timer.Concept is null && main.Timer.DisplayTime == "02:00",
            "The standalone timer opens from the home screen without a concept");
        main.Timer.ToggleCommand.Execute(null);
        clock.Advance(TimeSpan.FromSeconds(15));
        main.ReturnFromTimerCommand.Execute(null);
        check(!main.Timer.IsRunning && main.IsListsVisible && !main.IsTimerVisible,
            "Returning from the standalone timer stops it and restores the home screen");
        main.OpenTimerCommand.Execute(null);
        check(main.Timer.DisplayTime == "02:00" && !main.Timer.IsRunning, "Opening the timer again prepares a fresh countdown");
        main.ReturnFromTimerCommand.Execute(null);

        main.OpenSettingsCommand.Execute(null);
        main.DraftTimerMinutes = 60;
        main.SaveSettingsCommand.Execute(null);
        check(main.IsShowingSettings && !main.CanSaveTimerSettings && settings.LoadTimerMinutes() == 2,
            "Settings cannot confirm an out-of-range duration");
        main.DraftTimerMinutes = 3;
        main.SaveSettingsCommand.Execute(null);
        check(!main.IsDialogOpen && main.ConfiguredTimerMinutes == 3
            && new SettingsService(settingsPath).LoadTimerMinutes() == 3,
            "Back from settings saves the duration for future sessions");
        main.OpenSettingsCommand.Execute(null);
        main.DraftTimerMinutes = 4;
        main.CancelDialogCommand.Execute(null);
        check(main.ConfiguredTimerMinutes == 3 && settings.LoadTimerMinutes() == 3, "Canceling settings discards unsaved duration changes");

        string? warning = null;
        main.WarningRequested += (key, _) => warning = key;
        main.OpenSettingsCommand.Execute(null);
        main.DraftTimerMinutes = 5;
        using (var locked = File.Open(settingsPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            main.SaveSettingsCommand.Execute(null);
            check(main.IsShowingSettings && main.ConfiguredTimerMinutes == 3 && warning == "Timer.SettingsWriteError",
                "A failed settings save keeps the dialog and preserves the previous duration");
        }
        main.CancelDialogCommand.Execute(null);

        files.CreateList("Práctica");
        files.AddConcept("Práctica", "Explicar");
        main.LoadLists();
        main.OpenListCommand.Execute("Práctica");
        main.SelectRandomConceptCommand.Execute(null);
        main.OpenConceptTimerCommand.Execute(null);
        check(main.IsTimerVisible && !main.IsDialogOpen && !main.IsListDetailVisible
            && main.Timer.Concept == "Explicar" && main.Timer.DisplayTime == "03:00",
            "Opening the timer from a drawn concept transfers its text and configured duration");
        main.Timer.ToggleCommand.Execute(null);
        clock.Advance(TimeSpan.FromSeconds(20));
        main.ReturnFromTimerCommand.Execute(null);
        check(!main.Timer.IsRunning && main.IsShowingSelectedConcept && main.SelectedConcept == "Explicar"
            && main.LastSeenConcept == "Explicar" && files.LoadConcepts("Práctica").Single().SeenOrder == 1,
            "Returning restores the selected-concept dialog without another draw or progress change");
        main.CancelDialogCommand.Execute(null);

        File.WriteAllText(settingsPath, "Key,Value\nLanguage,en\nTimerMinutes,120\n");
        string invalidSettings = File.ReadAllText(settingsPath);
        var invalidMain = new MainViewModel(files, settings: settings);
        invalidMain.WarningRequested += (key, _) => warning = key;
        invalidMain.LoadTimerSettings();
        check(invalidMain.ConfiguredTimerMinutes == 1 && warning == "Timer.SettingsReadError"
            && File.ReadAllText(settingsPath) == invalidSettings,
            "An invalid saved duration reports a warning and preserves the file while using the default");
    }

    private sealed class ManualClock : TimeProvider
    {
        private long timestamp;
        public override long TimestampFrequency => TimeSpan.TicksPerSecond;
        public override long GetTimestamp() => timestamp;
        public void Advance(TimeSpan elapsed) => timestamp += elapsed.Ticks;
    }
}
