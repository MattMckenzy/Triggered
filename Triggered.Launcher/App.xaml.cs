using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Triggered.Launcher.Extensions;
using Triggered.Launcher.Models;

namespace Triggered.Launcher
{
    public partial class App : Application
    {
        private TaskbarIcon? NotifyIcon { get; set; } = null;
        private CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public Process? TriggeredProcess { get; set; } = null;
        public ConsoleContent ConsoleContent { get; set; } = new();
        public Uri Uri { get; set; } = new("https://localhost");

        protected async override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            NotifyIcon = (TaskbarIcon)FindResource("NotifyIcon");
            await CheckForUpdates();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            NotifyIcon?.Dispose();

            if (TriggeredProcess != null)
            {
                TriggeredProcess.StandardInput.Close();
                TriggeredProcess.CloseMainWindow();
                TriggeredProcess.Kill();
            }

            base.OnExit(e);
        }


        public async Task<bool> CheckForUpdates()
        {
            try
            {
                CancellationTokenSource = new CancellationTokenSource();
                HttpClient client = new();
                HttpResponseMessage responseMessage = await client.GetAsync("https://raw.githubusercontent.com/MattMckenzy/Triggered/main/Releases/win-x64/latest");

                Version onlineVersion = new(await responseMessage.Content.ReadAsStringAsync() ?? "0.0.0");
                ConsoleContent.WriteLine($"Latest version available: v{onlineVersion}");

                string localPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])!;
                FileInfo localExecutable = new(Path.Combine(localPath, "Triggered", "Triggered.exe"));
                Version localVersion = new("0.0.0");
                if (localExecutable.Exists)                
                    localVersion = new Version(FileVersionInfo.GetVersionInfo(localExecutable.FullName).FileVersion ?? "0.0.0");

                FileInfo localAppsettingsFile = new(Path.Combine(localPath, "Triggered", "appsettings.json"));
                if (localAppsettingsFile.Exists)
                {
                    JObject? localSettingsJson = JObject.Parse(File.ReadAllText(localAppsettingsFile.FullName));
                    string? url = localSettingsJson?.SelectToken("Kestrel.Endpoints.Https.Url")?.ToString();
                    if (url != null)
                        Uri = new Uri(url.Replace("*", "localhost").Replace("+", "localhost"));
                }

                if (localVersion == new Version("0.0.0"))
                    ConsoleContent.WriteLine($"Current version: none found!");
                else
                    ConsoleContent.WriteLine($"Current version: v{localVersion}");

                if (localVersion < onlineVersion)
                {
                    string zipPath = Path.Combine(localPath, $"{onlineVersion}.zip");

                    ConsoleContent.WriteLine(string.Empty);
                    ConsoleContent.WriteLine($"Updating Triggered.");
                    ConsoleContent.WriteLine($"Download starting...");
                    ConsoleContent.WriteLine(string.Empty);

                    Progress<(long, long)> progress = new();
                    Task downloadTask = Task.Run(async () =>
                    {
                        HttpClient client = new();
                        client.Timeout = TimeSpan.FromMinutes(5);
                        using FileStream file = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await client.DownloadAsync($"https://github.com/MattMckenzy/Triggered/raw/main/Releases/win-x64/{onlineVersion}.zip", file, progress, CancellationTokenSource.Token);
                    });

                    void Progress_ProgressChanged(object? sender, (long, long) e)
                    {
                        float currentMB = (float)e.Item1 / 1048576;
                        float totalMB = (float)e.Item2 / 1048576;
                        int percentage = (int)(currentMB * 100 / totalMB);
                        int fill = (int)Math.Floor((double)(percentage / 2));

                        lock (ConsoleContent)
                        {
                            ConsoleContent.EraseLines(2);
                            ConsoleContent.WriteLine($"Progress: {currentMB:n2} of {totalMB:n2}MB downloaded.");
                            ConsoleContent.WriteLine($"{new string('█', fill)}{new string('═', 50 - fill)} {percentage}%");
                        }
                    }

                    progress.ProgressChanged += Progress_ProgressChanged;

                    while (!downloadTask.IsCompleted)
                    {
                        await Task.Delay(500, CancellationTokenSource.Token);
                    }

                    await downloadTask;

                    ConsoleContent.WriteLine(string.Empty);
                    ConsoleContent.WriteLine($"Download successful!");
                    ConsoleContent.WriteLine($"Updating Triggered");

                    ZipFile.ExtractToDirectory(zipPath, localPath, true);
                    File.Delete(zipPath);

                    FileInfo currentLauncher = new(Path.Combine(localPath, "Triggered.Launcher.exe"));
                    FileInfo newLauncher = new(Path.Combine(localPath, "Triggered", "Triggered.Launcher.exe"));
                    string currentLauncherPath = currentLauncher.FullName;
                    File.Delete(currentLauncherPath.Replace("exe", "bak"));
                    currentLauncher.MoveTo(currentLauncherPath.Replace("exe", "bak"));
                    newLauncher.MoveTo(currentLauncherPath);

                    ConsoleContent.WriteLine($"Update Complete.");
                }

                ConsoleContent.WriteLine(string.Empty);
                ConsoleContent.WriteLine($"Starting Triggered.");
                ConsoleContent.WriteLine(string.Empty);

                ProcessStartInfo processStartInfo = new()
                {
                    FileName = Path.Combine(localPath, "Triggered", "Triggered.exe"),
                    WorkingDirectory = Path.Combine(localPath, "Triggered"),
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                TriggeredProcess = new()
                {
                    StartInfo = processStartInfo
                };

                TriggeredProcess.OutputDataReceived += TriggeredProcess_DataReceived;
                TriggeredProcess.ErrorDataReceived += TriggeredProcess_DataReceived;
                TriggeredProcess.Exited += TriggeredProcess_Exited;
                TriggeredProcess.Start();
                TriggeredProcess.BeginOutputReadLine();
                TriggeredProcess.BeginErrorReadLine();
            }
            catch (Exception exception)
            {
                ConsoleContent.WriteLine($"Couldn't get updates: {exception.Message}");
            }

            return false;
        }

        private void TriggeredProcess_Exited(object? sender, EventArgs e)
        {
            ConsoleContent.WriteLine(string.Empty);
            ConsoleContent.WriteLine($"Triggered service has stopped.");
            TriggeredProcess?.Dispose();
            TriggeredProcess = null;
        }

        private void TriggeredProcess_DataReceived(object sender, DataReceivedEventArgs e)
        {
            ConsoleContent.WriteLine(e.Data ?? string.Empty);
        }
    }
}
