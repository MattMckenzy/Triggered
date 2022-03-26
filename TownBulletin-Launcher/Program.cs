// See https://aka.ms/new-console-template for more information
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using TownBulletinLauncher;

await CheckForUpdates();

static async Task<bool> CheckForUpdates()
{
    object sync = new();

    try
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            HttpClient client = new();
            HttpResponseMessage responseMessage = await client.GetAsync("https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/Releases/win-x64/latest");
            
            Version onlineVersion = new(await responseMessage.Content.ReadAsStringAsync() ?? "0.0.0");
            Console.WriteLine($"Latest version available: v{onlineVersion}");

            string localPath = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])!;
            FileInfo localVersionFile = new(Path.Combine(localPath, "TownBulletin", "version.json"));
            Version localVersion = new("0.0.0");
            if (localVersionFile.Exists)
            {
                JsonNode? localVersionJson = JsonNode.Parse(File.ReadAllText(localVersionFile.FullName));
                localVersion = new(localVersionJson?["version"]?.ToString() ?? "0.0.0");
            }

            if (localVersion == new Version("0.0.0"))
                Console.WriteLine($"Current version: none found!");
            else
                Console.WriteLine($"Current version: v{localVersion}");

            if (localVersion < onlineVersion)
            {
                string zipPath = Path.Combine(localPath, $"{onlineVersion}.zip");

                Console.WriteLine();
                Console.WriteLine($"Updating TownBulletin.");
                Console.WriteLine($"Download starting...");
                Console.WriteLine();
                Console.CursorVisible = false;

                Progress<(long, long)> progress = new();
                CancellationTokenSource cancellationTokenSource = new();
                Task downloadTask = Task.Run(async () =>
                {
                    HttpClient client = new();
                    client.Timeout = TimeSpan.FromMinutes(5);
                    using FileStream file = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);
                    await client.DownloadAsync($"https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/Releases/win-x64/{onlineVersion}.zip", file, progress, cancellationTokenSource.Token);
                });

                void Progress_ProgressChanged(object? sender, (long, long) e)
                {
                    float currentMB = (float)e.Item1 / 1048576;
                    float totalMB = (float)e.Item2 / 1048576;
                    int percentage = (int)(currentMB * 100 / totalMB);
                    int fill = (int)Math.Floor((double)(percentage / 2));

                    lock (sync)
                    {
                        Console.WriteLine($"Progress: {currentMB.ToString("n2")} of {totalMB.ToString("n2")}MB downloaded.");
                        Console.WriteLine($"{new string('█', fill)}{new string('░', 50 - fill)} {percentage}%");
                        Console.SetCursorPosition(0, Console.CursorTop - 2);
                    }
                }

                progress.ProgressChanged += Progress_ProgressChanged;

                while (!downloadTask.IsCompleted)
                {
                    await Task.Delay(500, cancellationTokenSource.Token);
                }

                await downloadTask;

                Console.CursorVisible = true;
                Console.SetCursorPosition(0, Console.CursorTop + 2);
                Console.WriteLine();
                Console.WriteLine($"Download successful!");
                Console.WriteLine($"Updating TownBulletin");

                ZipFile.ExtractToDirectory(zipPath, localPath, true);
                File.Delete(zipPath);

                Console.WriteLine($"Update Complete.");
                Console.WriteLine();
            }

            Console.WriteLine($"Starting TownBulletin.");

            ProcessStartInfo processStartInfo = new()
            {
                FileName = Path.Combine(localPath, "TownBulletin", "TownBulletin.exe"),
                WorkingDirectory = Path.Combine(localPath, "TownBulletin"),
                CreateNoWindow = true
            };

            //TODO: Find a nice way to put service in tray.
            Process? townBulletinProcess = Process.Start(processStartInfo);

            if (townBulletinProcess != null)
                townBulletinProcess.WaitForExit();
        }
    }
    catch (Exception exception)
    {
        Console.Write($"Couldn't get updates: {exception.Message}");
    }

    return false;
}