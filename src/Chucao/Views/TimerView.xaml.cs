using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Chucao.ViewModels;

namespace Chucao.Views;

public partial class TimerView : UserControl
{
    private readonly DispatcherTimer refreshTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };

    public TimerView()
    {
        InitializeComponent();
        refreshTimer.Tick += (_, _) =>
        {
            if (DataContext is MainViewModel main)
                main.Timer.Update();
        };
    }

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            refreshTimer.Start();
            Dispatcher.BeginInvoke(new Action(() => StartButton.Focus()));
        }
        else
        {
            refreshTimer.Stop();
        }
    }

    private void View_Unloaded(object sender, RoutedEventArgs e) => refreshTimer.Stop();
}
