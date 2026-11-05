using System.Diagnostics;
using System.IO;
using System.Windows.Media;

namespace Chucao.Services;

// Shared playback behavior for the settings preview and the timer's completion sound.
public sealed class TimerSoundPlayer
{
    private readonly MediaPlayer player = new();

    public TimerSoundPlayer()
    {
        player.MediaEnded += (_, _) => Stop();
        player.MediaFailed += (_, e) => ReportFailure(e.ErrorException);
    }

    public event Action? PlaybackFailed;
    public double Volume { set => player.Volume = Math.Clamp(value, 0, 1); }

    public void Play(double volume)
    {
        Stop();
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds", "TimerFinished.wav");
        if (!File.Exists(path))
        {
            ReportFailure(new FileNotFoundException("Timer sound is missing.", path));
            return;
        }
        try
        {
            Volume = volume;
            player.Open(new Uri(path, UriKind.Absolute));
            player.Play();
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException)
        {
            ReportFailure(exception);
        }
    }

    private void ReportFailure(Exception exception)
    {
        Stop();
        Debug.WriteLine($"Timer sound could not be played: {exception.Message}");
        PlaybackFailed?.Invoke();
    }

    public void Stop() => player.Close();
}
