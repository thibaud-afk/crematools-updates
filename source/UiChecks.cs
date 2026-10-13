using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace CremaTools;

public static class UiChecks
{
    public static void Render(string folder) { var window = new MainWindow(); try { foreach (string name in new[] { "ShowHome", "ShowConvert", "ShowCompress", "ShowRename", "ShowScanner", "ShowVault", "ShowUpdates" }) { typeof(MainWindow).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(window, null); var content = (FrameworkElement)window.Content; if (content is Panel panel) panel.Background = new SolidColorBrush(Color.FromRgb(242, 245, 250)); content.Measure(new Size(1220, 820)); content.Arrange(new Rect(0, 0, 1220, 820)); content.UpdateLayout(); var bitmap = new RenderTargetBitmap(1220, 820, 96, 96, PixelFormats.Pbgra32); bitmap.Render(content); var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap)); using var stream = File.Create(Path.Combine(folder, name + ".png")); png.Save(stream); } } finally { window.Close(); } }
}
