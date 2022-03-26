using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using TownBulletin.Launcher.Commands;

namespace TownBulletin.Launcher
{
    public class NotifyIconViewModel
    {
        /// <summary>
        /// Shows a window, if none is already open.
        /// </summary>
        public static ICommand LaunchTownBulletinCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => ((App)Application.Current).TownBulletinProcess != null,

                    CommandAction = () =>
                    {
                        Process.Start(new ProcessStartInfo(((App)Application.Current).Uri.ToString()) { UseShellExecute = true });
                    }
                };
            }
        }

        public static ICommand ShowWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CanExecuteFunc = () => Application.Current.MainWindow == null ||
                        !Application.Current.MainWindow.IsVisible,

                    CommandAction = () =>
                    {
                        Application.Current.MainWindow = new MainWindow();
                        Application.Current.MainWindow.Show();
                    }
                };
            }
        }

        public static ICommand HideWindowCommand
        {
            get
            {
                return new DelegateCommand
                {
                    CommandAction = () => Application.Current.MainWindow.Close(),

                    CanExecuteFunc = () => Application.Current.MainWindow != null &&
                        Application.Current.MainWindow.IsVisible
                };
            }
        }


        public static ICommand ExitApplicationCommand
        {
            get
            {
                return new DelegateCommand { CommandAction = () => Application.Current.Shutdown() };
            }
        }
    }
}