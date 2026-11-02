using System.Windows;
using System.Windows.Controls;

namespace Chucao.Views;

public partial class AddConceptSetView : UserControl
{
    public AddConceptSetView() => InitializeComponent();

    private void View_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
            Dispatcher.BeginInvoke(new Action(() => ConceptSetInput.Focus()));
    }
}
