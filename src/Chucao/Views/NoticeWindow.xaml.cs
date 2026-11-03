using System.Windows;
using Chucao.Services;

namespace Chucao.Views;

public partial class NoticeWindow : Window
{
    public NoticeWindow(LocalizationService localization, string message)
    {
        InitializeComponent();
        NativeWindowColors.Attach(this);
        DataContext = localization;
        MessageText.Text = message;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
