using System.ComponentModel;
using System.Globalization;
using System.Windows.Input;
using Chucao.Services;

namespace Chucao.ViewModels;

public sealed class TimerViewModel : INotifyPropertyChanged
{
    private readonly LocalizationService localization;
    private readonly TimeProvider timeProvider;
    private TimeSpan duration = TimeSpan.FromMinutes(1);
    private TimeSpan remaining = TimeSpan.FromMinutes(1);
    private TimeSpan remainingAtStart;
    private long startedAt;
    private bool hasStarted;
    private string? concept;

    public TimerViewModel(LocalizationService localization, TimeProvider? timeProvider = null)
    {
        this.localization = localization;
        this.timeProvider = timeProvider ?? TimeProvider.System;
        ToggleCommand = new RelayCommand(_ => Toggle());
        localization.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(LocalizationService.CurrentLanguage))
                Notify(nameof(ActionText));
        };
    }

    public string? Concept => concept;
    public bool IsRunning { get; private set; }
    public bool IsFinished => remaining == TimeSpan.Zero;
    public TimeSpan Remaining => remaining;
    public string DisplayTime
    {
        get
        {
            int seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", seconds / 60, seconds % 60);
        }
    }
    public string ActionText => localization[IsFinished ? "Timer.Restart"
        : IsRunning ? "Timer.Pause" : hasStarted ? "Timer.Resume" : "Timer.Start"];
    public ICommand ToggleCommand { get; }

    public void Prepare(int minutes, string? selectedConcept)
    {
        if (minutes is < 1 or > 59)
            throw new ArgumentOutOfRangeException(nameof(minutes));
        duration = TimeSpan.FromMinutes(minutes);
        remaining = duration;
        concept = selectedConcept;
        IsRunning = false;
        hasStarted = false;
        Notify(nameof(Concept));
        NotifyState();
    }

    private void Toggle()
    {
        if (IsRunning)
        {
            Pause();
            return;
        }
        if (IsFinished)
            remaining = duration;
        remainingAtStart = remaining;
        startedAt = timeProvider.GetTimestamp();
        hasStarted = true;
        IsRunning = true;
        NotifyState();
    }

    public void Update()
    {
        if (!IsRunning)
            return;
        // Use elapsed monotonic time, not a count of UI ticks, to avoid accumulating drift.
        TimeSpan elapsed = timeProvider.GetElapsedTime(startedAt);
        remaining = elapsed >= remainingAtStart ? TimeSpan.Zero : remainingAtStart - elapsed;
        if (IsFinished)
            IsRunning = false;
        NotifyState();
    }

    public void Pause()
    {
        Update();
        IsRunning = false;
        NotifyState();
    }

    private void NotifyState()
    {
        Notify(nameof(IsRunning));
        Notify(nameof(IsFinished));
        Notify(nameof(Remaining));
        Notify(nameof(DisplayTime));
        Notify(nameof(ActionText));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
