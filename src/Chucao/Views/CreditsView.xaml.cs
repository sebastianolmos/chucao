using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Chucao.ViewModels;

namespace Chucao.Views;

public partial class CreditsView : UserControl
{
    public CreditsView() => InitializeComponent();

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(new Action(() => BackButton.Focus()));
    }

    private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        e.Handled = true;
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            if (DataContext is MainViewModel main)
                new NoticeWindow(main.Localization, main.Localization["Credits.LinkError"])
                { Owner = Window.GetWindow(this) }.ShowDialog();
        }
    }
}
