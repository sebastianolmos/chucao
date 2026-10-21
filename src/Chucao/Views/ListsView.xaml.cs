using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using Chucao.ViewModels;

namespace Chucao.Views;

public partial class ListsView : UserControl
{
    public ListsView() => InitializeComponent();

    public void FocusAddButton() => AddListButton.Focus();

    private void View_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel previous)
            previous.Lists.CollectionChanged -= Lists_CollectionChanged;
        if (e.NewValue is MainViewModel current)
            current.Lists.CollectionChanged += Lists_CollectionChanged;
    }

    private void Lists_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Add && DataContext is MainViewModel { IsCreatingList: true })
            Dispatcher.BeginInvoke(new Action(ListsScrollViewer.ScrollToEnd));
    }
}
