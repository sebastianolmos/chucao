using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Chucao.ViewModels;

namespace Chucao.Views;

public partial class TimerView : UserControl
{
    // Relative (X, Y) coordinates: (0, 0) is top-left; (1, 1) is bottom-right.
    // Add or remove entries here to change the flight route and temporary markers.
    private readonly Point[] animationWaypoints =
    [
        new(0.38, 0.23),
        new(0.75, 0.32),
        new(0.23, 0.42),
        new(0.82, 0.62),
        new(0.20, 0.75),
    ];

    // Seconds: work backwards from the final flight, adding the increment per leg.
    private const double FinalFlightSeconds = 2;
    private const double EarlierFlightIncrementSeconds = 2;

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
        DrawWaypointMarkers(e.NewSize);
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

    private void DrawWaypointMarkers(Size canvasSize)
    {
        WaypointLayer.Children.Clear();
        double diameter = (double)FindResource("AnimationWaypointDiameter");

        foreach (Point waypoint in animationWaypoints)
        {
            var marker = new Ellipse { Width = diameter, Height = diameter };
            marker.SetResourceReference(Shape.FillProperty, "TextBrush");
            // Center each marker on its relative position within the whole canvas.
            Canvas.SetLeft(marker, waypoint.X * canvasSize.Width - diameter / 2);
            Canvas.SetTop(marker, waypoint.Y * canvasSize.Height - diameter / 2);
            WaypointLayer.Children.Add(marker);
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
        // The source artwork faces left; flip once for every completed flight.
        BirdDirection.ScaleX = frame.FacingLeft ? 1 : -1;
        // Future wing frames should animate only while IsRunning and frame.State == Flying.
    }

    private void StopPlaybackAndRefresh()
    {
        CompositionTarget.Rendering -= RenderAnimation;
        countdownRefreshTimer.Stop();
        completionSound.Close();
    }

    private void View_Unloaded(object sender, RoutedEventArgs e) => StopPlaybackAndRefresh();
}
