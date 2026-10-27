using lab6.Tests;
using System.Configuration;
using System.Data;
using System.Windows;

namespace lab6
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            MatrixTests.RunAllTests();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }

}
