using System.Windows;
namespace CremaTools;

public partial class App : Application { }
public static class Entry
{
    [STAThread]
    public static void Main(string[] args)
    {
        var application = new App(); application.InitializeComponent();
        if (args.Contains("--self-test")) { Environment.SetEnvironmentVariable("CREMATOOLS_TEST_HOME", System.IO.Path.Combine(AppContext.BaseDirectory, "test-results", "user-data-" + Guid.NewGuid().ToString("N"))); try { SelfTests.RunAsync().GetAwaiter().GetResult(); Environment.Exit(0); } catch (Exception e) { System.IO.File.WriteAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "self-test-error.txt"), e.ToString()); Environment.Exit(1); } return; }
        application.DispatcherUnhandledException += (_, e) => { MessageBox.Show(e.Exception.Message, "CrémaTools", MessageBoxButton.OK, MessageBoxImage.Error); e.Handled = true; };
        application.Run(new MainWindow());
    }
}
