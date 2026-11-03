using System.Configuration;
using System.Data;
using System.Windows;
using System.IO;
using System.Windows.Media.Imaging;

namespace Chucao;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LoadOptionalImage("ApplicationImage", "Assets/Images/Application.png");
        LoadOptionalImage("ApplicationWindowIcon", "Assets/Images/Application.ico");
        base.OnStartup(e);
    }

    private void LoadOptionalImage(string key, string resourcePath)
    {
        try
        {
            using var stream = GetResourceStream(new Uri("/Chucao;component/" + resourcePath, UriKind.Relative))?.Stream;
            if (stream is null)
                return;
            // BitmapFrame retains the ICO decoder so WPF can choose the appropriate icon size.
            var image = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            image.Freeze();
            Resources[key] = image;
        }
        catch (IOException)
        {
            // Ignored local preview images are optional in other checkouts.
        }
    }
}

