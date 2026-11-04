using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Chucao.ViewModels;

namespace Chucao.Views;

public partial class TimerView : UserControl
{
    // Relative (X, Y) coordinates: (0, 0) is top-left; (1, 1) is bottom-right.
    // Add or remove entries here to change the flight route.
    private readonly Point[] animationWaypoints =
    [
        new(0.34, 0.23),
        new(0.75, 0.32),
        new(0.23, 0.42),
        new(0.86, 0.62),
        new(0.18, 0.77),
    ];

    // X is the horizontal center, Y is the top edge, Width is a fraction of canvas width.
    // Entries follow the waypoint order; Flip mirrors the right-hand stumps horizontally.
    private readonly (double X, double Y, double Width, bool Flip)[] stumpPlacements =
    [
        (0.34, 0.25, 0.17, false),
        (0.75, 0.33, 0.20, true),
        (0.20, 0.46, 0.23, false),
        (0.86, 0.67, 0.28, true),
        (0.15, 0.80, 0.35, false),
    ];

    // Seconds: work backwards from the final flight, adding the increment per leg.
    private const double FinalFlightSeconds = 1;
    private const double EarlierFlightIncrementSeconds = 1;
    // Seconds per wing image: lower values produce faster flapping. Keep greater than 0.
    private const double FlightFrameSeconds = 0.1;

    // Volume ranges from 0 (silent) to 1 (maximum); Windows volume also applies.
    private const double CompletionSoundVolume = 0.6;
    private readonly MediaPlayer completionSound = new();
    private bool completionSoundPlayed;
    // Keep checking the countdown even when Windows stops rendering a minimized window.
    private readonly DispatcherTimer countdownRefreshTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public TimerView()
    {
        InitializeComponent();
        countdownRefreshTimer.Tick += RenderAnimation;
        completionSound.MediaEnded += (_, _) => completionSound.Close();
        completionSound.MediaFailed += (_, e) => Debug.WriteLine($"Timer sound could not be played: {e.ErrorException.Message}");
    }

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            CompositionTarget.Rendering -= RenderAnimation;
            CompositionTarget.Rendering += RenderAnimation;
            countdownRefreshTimer.Start();
            UpdateBird();
            Dispatcher.BeginInvoke(new Action(() => StartButton.Focus()));
        }
        else
        {
            StopPlaybackAndRefresh();
        }
    }

    private void AnimationCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Clip both the background and future scene elements to the rounded canvas.
        AnimationClip.Rect = new Rect(new Point(0, 0), e.NewSize);
        DrawStumps(e.NewSize);
        UpdateBird();
    }

    private void UpdateCompletionSound()
    {
        if (DataContext is not MainViewModel main)
            return;
        if (!main.Timer.IsFinished)
        {
            if (completionSoundPlayed)
                completionSound.Close();
            completionSoundPlayed = false;
            return;
        }
        if (completionSoundPlayed)
            return;

        // Mark this countdown before playback, so render/timer ticks cannot repeat the sound.
        completionSoundPlayed = true;
        string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds", "TimerFinished.wav");
        if (!File.Exists(path))
            return; // The ignored preview asset is optional in other checkouts.
        try
        {
            completionSound.Volume = CompletionSoundVolume;
            completionSound.Open(new Uri(path, UriKind.Absolute));
            completionSound.Play();
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException)
        {
            Debug.WriteLine($"Timer sound could not be played: {exception.Message}");
        }
    }

    private void DrawStumps(Size canvasSize)
    {
        StumpLayer.Children.Clear();
        var source = (ImageSource)FindResource("StumpImage");
        foreach (var placement in stumpPlacements)
        {
            double width = placement.Width * canvasSize.Width;
            var stump = new Image
            {
                Source = source,
                Width = width,
                Height = width * source.Height / source.Width,
                Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(placement.Flip ? -1 : 1, 1)
            };
            Canvas.SetLeft(stump, placement.X * canvasSize.Width - width / 2);
            Canvas.SetTop(stump, placement.Y * canvasSize.Height);
            StumpLayer.Children.Add(stump);
        }
    }

    private void RenderAnimation(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel main)
            main.Timer.Update();
        UpdateBird();
        UpdateCompletionSound();
    }

    private void UpdateBird()
    {
        if (DataContext is not MainViewModel main || animationWaypoints.Length == 0)
        {
            BirdImage.Visibility = Visibility.Collapsed;
            return;
        }

        BirdImage.Visibility = Visibility.Visible;
        BirdFlightFrame frame = BirdFlightTimeline.Evaluate(main.Timer.Duration.TotalSeconds,
            main.Timer.Elapsed.TotalSeconds, animationWaypoints.Length,
            FinalFlightSeconds, EarlierFlightIncrementSeconds);
        string imageKey = "ApplicationImage";
        if (main.Timer.IsRunning && frame.State == BirdMotionState.Flying)
        {
            // The shared elapsed clock freezes wing animation while the timer is paused.
            long wingFrame = (long)(main.Timer.Elapsed.TotalSeconds / FlightFrameSeconds);
            imageKey = wingFrame % 2 == 0 ? "BirdFly1Image" : "BirdFly2Image";
        }
        var image = (ImageSource)FindResource(imageKey);
        if (!ReferenceEquals(BirdImage.Source, image))
            BirdImage.Source = image;
        Point from = animationWaypoints[frame.FromIndex];
        Point to = animationWaypoints[frame.ToIndex];
        double x = from.X + (to.X - from.X) * frame.FlightProgress;
        double y = from.Y + (to.Y - from.Y) * frame.FlightProgress;
        double initialSize = (double)FindResource("BirdInitialSize");
        double finalSize = (double)FindResource("BirdFinalSize");
        double size = initialSize + (finalSize - initialSize) * frame.GrowthProgress;
        BirdImage.Width = size;
        BirdImage.Height = size;
        Canvas.SetLeft(BirdImage, x * AnimationCanvas.ActualWidth - size / 2);
        Canvas.SetTop(BirdImage, y * AnimationCanvas.ActualHeight - size / 2);
        // Keep the original right-facing pose first, then flip at each arrival.
        BirdDirection.ScaleX = frame.FacingRight ? 1 : -1;
    }

    private void StopPlaybackAndRefresh()
    {
        CompositionTarget.Rendering -= RenderAnimation;
        countdownRefreshTimer.Stop();
        completionSound.Close();
    }

    private void View_Unloaded(object sender, RoutedEventArgs e) => StopPlaybackAndRefresh();
}
