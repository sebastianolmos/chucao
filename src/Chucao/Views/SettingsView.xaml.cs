using System.Windows;
using System.Windows.Controls;

namespace Chucao.Views;

public partial class SettingsView : UserControl
{
    public SettingsView() => InitializeComponent();

    public void FocusCreditsButton() => CreditsButton.Focus();

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(new Action(() => MinutesInput.Focus()));
    }
}
