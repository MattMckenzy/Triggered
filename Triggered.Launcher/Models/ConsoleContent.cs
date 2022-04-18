using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace Triggered.Launcher.Models
{
    public class ConsoleContent : INotifyPropertyChanged
    {
        readonly ObservableCollection<string> _consoleOutput = new() 
        { 
            $"TR⚡GGERED Launcher v{System.Diagnostics.FileVersionInfo.GetVersionInfo(Environment.ProcessPath!).FileVersion}.", 
            "" 
        };

        public ObservableCollection<string> ConsoleOutput
        {
            get
            {
                return _consoleOutput;
            }
        }

        public void WriteLine(string line)
        {
            Application.Current?.Dispatcher?.Invoke(delegate
            {
                _consoleOutput.Add(line);
                OnPropertyChanged(nameof(ConsoleOutput));
            });
        }

        public void EraseLines(int count)
        {
            Application.Current.Dispatcher.Invoke(delegate
            {
                for (int i = 0; i < count; i++)
                    _consoleOutput.RemoveAt(_consoleOutput.Count - 1);
                OnPropertyChanged(nameof(ConsoleOutput));
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
