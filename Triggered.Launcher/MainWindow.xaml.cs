using System.Windows;
using Triggered.Launcher.Models;

namespace Triggered.Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ConsoleContent consoleContent = ((App)Application.Current).ConsoleContent;
            DataContext = consoleContent;
            consoleContent.PropertyChanged += ConsoleContent_PropertyChanged;
        }

        private void ConsoleContent_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Scroller.ScrollToEnd();            
        }
    }
}
