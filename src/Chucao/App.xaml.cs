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
        LoadImage("ApplicationImage", "Assets/Images/Application.png");
        LoadImage("ApplicationWindowIcon", "Assets/Images/Application.ico");
        LoadImage("BirdFly1Image", "Assets/Images/BirdFly1.png");
        LoadImage("BirdFly2Image", "Assets/Images/BirdFly2.png");
        LoadImage("StumpImage", "Assets/Images/Stump.png");
        base.OnStartup(e);
    }

    private void LoadImage(string key, string resourcePath)
    {
        using var stream = GetResourceStream(new Uri("/Chucao;component/" + resourcePath, UriKind.Relative))!.Stream;
        // BitmapFrame retains the ICO decoder so WPF can choose the appropriate icon size.
        var image = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        image.Freeze();
        Resources[key] = image;
    }
}

