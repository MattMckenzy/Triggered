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
using TownBulletin.Launcher.Extensions;
using TownBulletin.Launcher.Models;

namespace TownBulletin.Launcher
{
    public partial class App : Application
    {
        private TaskbarIcon? NotifyIcon { get; set; } = null;
        private CancellationTokenSource CancellationTokenSource { get; set; } = new();
        public Process? TownBulletinProcess { get; set; } = null;
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

            if (TownBulletinProcess != null)
            {
                TownBulletinProcess.StandardInput.Close();
                TownBulletinProcess.CloseMainWindow();
                TownBulletinProcess.Kill();
            }

            base.OnExit(e);
        }


        public async Task<bool> CheckForUpdates()
        {
            try
            {
                CancellationTokenSource = new CancellationTokenSource();
                HttpClient client = new();
                HttpResponseMessage responseMessage = await client.GetAsync("https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/Releases/win-x64/latest");

                Version onlineVersion = new(await responseMessage.Content.ReadAsStringAsync() ?? "0.0.0");
                ConsoleContent.WriteLine($"Latest version available: v{onlineVersion}");

                string localPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])!;
                FileInfo localExecutable = new(Path.Combine(localPath, "TownBulletin", "TownBulletin.exe"));
                Version localVersion = new("0.0.0");
                if (localExecutable.Exists)                
                    localVersion = new Version(FileVersionInfo.GetVersionInfo(localExecutable.FullName).FileVersion ?? "0.0.0");

                FileInfo localAppsettingsFile = new(Path.Combine(localPath, "TownBulletin", "appsettings.json"));
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
                    ConsoleContent.WriteLine($"Updating TownBulletin.");
                    ConsoleContent.WriteLine($"Download starting...");
                    ConsoleContent.WriteLine(string.Empty);

                    Progress<(long, long)> progress = new();
                    Task downloadTask = Task.Run(async () =>
                    {
                        HttpClient client = new();
                        client.Timeout = TimeSpan.FromMinutes(5);
                        using FileStream file = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await client.DownloadAsync($"https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/Releases/win-x64/{onlineVersion}.zip", file, progress, CancellationTokenSource.Token);
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
                    ConsoleContent.WriteLine($"Updating TownBulletin");

                    ZipFile.ExtractToDirectory(zipPath, localPath, true);
                    File.Delete(zipPath);

                    FileInfo currentLauncher = new(Path.Combine(localPath, "TownBulletin.Launcher.exe"));
                    FileInfo newLauncher = new(Path.Combine(localPath, "TownBulletin", "TownBulletin.Launcher.exe"));
                    currentLauncher.MoveTo(currentLauncher.FullName.Replace("exe", "bak"));
                    newLauncher.MoveTo(currentLauncher.FullName);

                    ConsoleContent.WriteLine($"Update Complete.");
                }

                ConsoleContent.WriteLine(string.Empty);
                ConsoleContent.WriteLine($"Starting TownBulletin.");
                ConsoleContent.WriteLine(string.Empty);

                ProcessStartInfo processStartInfo = new()
                {
                    FileName = Path.Combine(localPath, "TownBulletin", "TownBulletin.exe"),
                    WorkingDirectory = Path.Combine(localPath, "TownBulletin"),
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                TownBulletinProcess = new()
                {
                    StartInfo = processStartInfo
                };

                TownBulletinProcess.OutputDataReceived += TownBulletinProcess_DataReceived;
                TownBulletinProcess.ErrorDataReceived += TownBulletinProcess_DataReceived;
                TownBulletinProcess.Exited += TownBulletinProcess_Exited;
                TownBulletinProcess.Start();
                TownBulletinProcess.BeginOutputReadLine();
                TownBulletinProcess.BeginErrorReadLine();
            }
            catch (Exception exception)
            {
                ConsoleContent.WriteLine($"Couldn't get updates: {exception.Message}");
            }

            return false;
        }

        private void TownBulletinProcess_Exited(object? sender, EventArgs e)
        {
            ConsoleContent.WriteLine(string.Empty);
            ConsoleContent.WriteLine($"TownBulletin service has stopped.");
            TownBulletinProcess?.Dispose();
            TownBulletinProcess = null;
        }

        private void TownBulletinProcess_DataReceived(object sender, DataReceivedEventArgs e)
        {
            ConsoleContent.WriteLine(e.Data ?? string.Empty);
        }
    }
}
