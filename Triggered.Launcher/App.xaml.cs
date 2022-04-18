using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
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
                string localPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])!;
                FileInfo currentLauncher = new(Environment.GetCommandLineArgs()[0]);
                FileInfo newLauncher = new(Environment.GetCommandLineArgs()[0] + ".new");
                if (newLauncher.Exists)
                {
                    ConsoleContent.WriteLine($"There is a new version of the launcher available in the current directory, please replace \"{currentLauncher.Name}\" with \"{newLauncher.Name}\" by removing \".new\" from the file name and launch it again.");
                }    

                CancellationTokenSource = new CancellationTokenSource();
                HttpClient client = new();
                HttpResponseMessage responseMessage = await client.GetAsync("https://raw.githubusercontent.com/MattMckenzy/Triggered/main/latestversion");

                Version onlineVersion = new(await responseMessage.Content.ReadAsStringAsync() ?? "0.0.0");
                ConsoleContent.WriteLine($"Latest version available: v{onlineVersion}");

                FileInfo localExecutable = new(Path.Combine(localPath, "Triggered", "Triggered.exe"));
                Version localVersion = new("0.0.0");
                if (localExecutable.Exists)                
                    localVersion = new Version(FileVersionInfo.GetVersionInfo(localExecutable.FullName).FileVersion ?? "0.0.0");

                if (localVersion == new Version("0.0.0"))
                    ConsoleContent.WriteLine($"Current version: none found!");
                else
                    ConsoleContent.WriteLine($"Current version: v{localVersion}");

                if (localVersion < onlineVersion)
                {
                    string zipPath = Path.Combine(localPath, $"{onlineVersion}.zip");

                    ConsoleContent.WriteLine(string.Empty);
                    ConsoleContent.WriteLine($"Updating service.");
                    ConsoleContent.WriteLine(string.Empty);
                    ConsoleContent.WriteLine($"Downloading...");
                    ConsoleContent.WriteLine(string.Empty);
                    ConsoleContent.WriteLine(string.Empty);

                    Progress<(long, long)> progress = new();
                    Task downloadTask = Task.Run(async () =>
                    {
                        HttpClient client = new();
                        client.Timeout = TimeSpan.FromMinutes(5);
                        using FileStream file = new(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                        await client.DownloadAsync($"https://github.com/MattMckenzy/Triggered/releases/download/v{onlineVersion}/{onlineVersion}.zip", file, progress, CancellationTokenSource.Token);
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

                    ConsoleContent.WriteLine($"Download successful!");
                    ConsoleContent.WriteLine(string.Empty);

                    Task unzipTask = Task.Run(async () =>
                    {
                        ConsoleContent.WriteLine("Starting update.");
                        ConsoleContent.WriteLine(string.Empty);

                        using ZipArchive zipArchive = ZipFile.Open(zipPath, ZipArchiveMode.Read);
                        foreach(ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
                        {
                            if (string.IsNullOrWhiteSpace(zipArchiveEntry.Name))
                                continue;

                            string entryRelativePath = zipArchiveEntry.FullName;
                            string entryExtension = Path.GetExtension(zipArchiveEntry.Name);
                            string currentFilePath = Path.Combine(localPath, entryRelativePath);

                            if (currentFilePath.Equals(currentLauncher.FullName, StringComparison.InvariantCultureIgnoreCase))
                            {
                                if (!File.ReadAllBytes(currentFilePath).SequenceEqual(zipArchiveEntry.GetBytes()))
                                {
                                    currentFilePath = newLauncher.FullName;
                                    ConsoleContent.EraseLines(1);
                                    ConsoleContent.WriteLine($"New launcher updated. Please replace \"{currentLauncher.Name}\" with \"{newLauncher.Name}\" by removing \".new\" from the file name before launching it again.");
                                    ConsoleContent.WriteLine(string.Empty);
                                }
                                else
                                    continue;
                            }
                            else if ((entryExtension.Equals(".cs", StringComparison.InvariantCultureIgnoreCase) ||
                                    entryExtension.Equals(".db", StringComparison.InvariantCultureIgnoreCase)) &&
                                File.Exists(currentFilePath) &&
                                !File.ReadAllBytes(currentFilePath).SequenceEqual(zipArchiveEntry.GetBytes()))
                            {
                                ConsoleContent.EraseLines(1);
                                ConsoleContent.WriteLine($"Updated file \"{entryRelativePath}\" is different than current version. Will not replace.");
                                ConsoleContent.WriteLine(string.Empty);
                                continue;
                            }

                            DirectoryInfo currentDirectory = new(Path.GetDirectoryName(currentFilePath)!);
                            while (!currentDirectory.Exists)
                            {
                                try
                                {
                                    currentDirectory.Create();
                                }
                                catch
                                {
                                    await Task.Delay(500);
                                }
                            }

                            zipArchiveEntry.ExtractToFile(currentFilePath, true);

                            ConsoleContent.EraseLines(1);
                            ConsoleContent.WriteLine($"Updated file \"{entryRelativePath}\".");
                        }
                  
                        ConsoleContent.EraseLines(1);
                        ConsoleContent.WriteLine("Update complete.");
                    });

                    while (!unzipTask.IsCompleted)
                    {
                        await Task.Delay(500, CancellationTokenSource.Token);
                    }

                    await unzipTask;
                  
                    File.Delete(zipPath);
                }

                FileInfo localAppsettingsFile = new(Path.Combine(localPath, "Triggered", "appsettings.json"));
                if (localAppsettingsFile.Exists)
                {
                    JObject? localSettingsJson = JObject.Parse(File.ReadAllText(localAppsettingsFile.FullName));
                    string? url = localSettingsJson?.SelectToken("Kestrel.Endpoints.HttpsInlineCertFile.Url")?.ToString();
                    if (url != null)
                        Uri = new Uri(url.Replace("*", "localhost").Replace("+", "localhost"));
                }

                ConsoleContent.WriteLine(string.Empty);
                ConsoleContent.WriteLine($"Starting service.");
                ConsoleContent.WriteLine(string.Empty);
                ConsoleContent.WriteLine("------------------------------");
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
            ConsoleContent.WriteLine($"Service has stopped.");
            TriggeredProcess?.Dispose();
            TriggeredProcess = null;
        }

        private void TriggeredProcess_DataReceived(object sender, DataReceivedEventArgs e)
        {
            ConsoleContent.WriteLine(e.Data ?? string.Empty);
        }      
        
    }
}
