using System.Windows;
using System.Windows.Controls;

namespace Chucao.Views;

public partial class SelectedConceptView : UserControl
{
    public SelectedConceptView() => InitializeComponent();

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(new Action(() => ReturnButton.Focus()));
    }
}
