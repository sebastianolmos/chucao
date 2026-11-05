using Chucao.Services;
using Chucao.ViewModels;

internal static class AudioSettingsChecks
{
    public static void Run(string temporaryDirectory, Action<bool, string> check)
    {
        string path = Path.Combine(temporaryDirectory, "audio-settings.csv");
        var settings = new SettingsService(path);
        check(settings.LoadTimerSoundVolume() == 0.6 && settings.LoadTimerSoundEnabled() && !File.Exists(path),
            "New audio preferences default to enabled at sixty percent without creating a file");
        File.WriteAllText(path, "Key,Value\nLanguage,es\nTimerMinutes,3\nNote,Keep\n");
        string legacy = File.ReadAllText(path);
        check(settings.LoadTimerSoundVolume() == 0.6 && settings.LoadTimerSoundEnabled()
            && File.ReadAllText(path) == legacy, "Old settings load audio defaults without rewriting user data");
        settings.SaveTimerSettings(4, 0.25, false);
        var reopened = new SettingsService(path);
        check(reopened.LoadTimerMinutes() == 4 && reopened.LoadTimerSoundVolume() == 0.25
            && !reopened.LoadTimerSoundEnabled() && reopened.LoadLanguage() == "es"
            && File.ReadAllText(path).Contains("Keep"),
            "Audio preferences persist with duration and preserve language and unrelated settings");
        string saved = File.ReadAllText(path);
        foreach (double invalid in new[] { -0.1, 1.1, double.NaN, double.PositiveInfinity })
        {
            bool rejected = false;
            try { settings.SaveTimerSettings(5, invalid, true); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            check(rejected && File.ReadAllText(path) == saved,
                "An invalid audio volume cannot partially overwrite saved preferences");
        }

        var main = new MainViewModel(settings: settings);
        main.LoadTimerSettings();
        main.OpenSettingsCommand.Execute(null);
        main.DraftTimerSoundVolume = 0.8;
        main.ToggleTimerSoundCommand.Execute(null);
        double? previewVolume = null;
        main.SoundPreviewRequested += volume => previewVolume = volume;
        main.DraftTimerSoundEnabled = false;
        main.TestTimerSoundCommand.Execute(null);
        check(previewVolume == 0.8 && !main.DraftTimerSoundEnabled,
            "Preview uses the draft volume even when completion sound is disabled");
        main.OpenCreditsCommand.Execute(null);
        check(main.IsShowingCredits && main.IsShowingSettings && !main.IsBaseDialogEnabled,
            "Credits overlay settings without discarding them");
        main.CancelDialogCommand.Execute(null);
        check(!main.IsShowingCredits && main.IsShowingSettings && main.DraftTimerSoundVolume == 0.8,
            "Escape closes only credits and preserves pending settings changes");
        main.CancelDialogCommand.Execute(null);
        main.OpenSettingsCommand.Execute(null);
        check(main.DraftTimerSoundVolume == 0.25 && !main.DraftTimerSoundEnabled,
            "Canceling settings discards pending audio changes");

        main.DraftTimerMinutes = 6;
        main.DraftTimerSoundVolume = 0;
        main.DraftTimerSoundEnabled = true;
        using (var locked = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            main.SaveSettingsCommand.Execute(null);
            check(main.IsShowingSettings && main.ConfiguredTimerMinutes == 4
                && main.ConfiguredTimerSoundVolume == 0.25 && !main.ConfiguredTimerSoundEnabled,
                "A failed save keeps the dialog open and preserves all active timer preferences");
        }
        main.SaveSettingsCommand.Execute(null);
        check(!main.IsDialogOpen && reopened.LoadTimerMinutes() == 6 && reopened.LoadTimerSoundVolume() == 0
            && reopened.LoadTimerSoundEnabled(), "Saving settings stores zero volume and enabled state independently");
        main.OpenSettingsCommand.Execute(null);
        main.Localization.CurrentLanguage = "es";
        string spanish = main.TimerSoundToggleText;
        main.OpenCreditsCommand.Execute(null);
        main.Localization.CurrentLanguage = "en";
        check(main.IsShowingCredits && main.DraftTimerSoundVolume == 0 && spanish != main.TimerSoundToggleText
            && main.Localization["Credits.Sound"].Contains("Gabriel Morales, XC870902"),
            "Changing language translates audio controls and credits without losing dialog state");

        foreach (string invalid in new[] { "TimerSoundVolume,NaN", "TimerSoundVolume,1.5", "TimerSoundEnabled,maybe" })
        {
            string csv = "Key,Value\nLanguage,en\n" + invalid + "\n";
            File.WriteAllText(path, csv);
            bool rejected = false;
            try { settings.LoadTimerSoundVolume(); settings.LoadTimerSoundEnabled(); }
            catch (InvalidDataException) { rejected = true; }
            check(rejected && File.ReadAllText(path) == csv,
                "Invalid saved audio preferences are rejected without altering the file");
        }
    }
}
