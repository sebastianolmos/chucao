using System.Windows;
using System.Windows.Controls;

namespace Chucao.Views;

public partial class CreateListView : UserControl
{
    public CreateListView() => InitializeComponent();

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(new Action(() => ListNameInput.Focus()));
    }
}
