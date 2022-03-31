using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using TownBulletin.Extensions;
using TownBulletin.Middleware;
using TownBulletin.Models;
using TownBulletin.Services;
using TwitchLib.EventSub.Webhooks.Extensions;

// **********************
//   Register services.
// **********************

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && configuration.GetValue<bool>("GenerateCertificate"))
    CreateCertificate(configuration["Kestrel:Endpoints:HttpsInlineCertFile:Certificate:Path"], 
        configuration["Kestrel:Endpoints:HttpsInlineCertFile:Certificate:Password"]);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(configuration.GetSection("Kestrel"), true);
});

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

configuration["TwitchSecret"] = 89.GetThisRandomStringLength();

builder.Services.AddTwitchLibEventSubWebhooks(config =>
{
    config.EnableLogging = true;
    config.Secret = configuration["TwitchSecret"];
    config.CallbackPath = "/twitch/events/webhook";
});

builder.Services.AddSingleton<MemoryCache>();
builder.Services.AddSingleton<DataService>();
builder.Services.AddSingleton<QueueService>();
builder.Services.AddSingleton<MessagingService>();
builder.Services.AddSingleton<TwitchService>();
builder.Services.AddSingleton<TwitchChatService>();
builder.Services.AddSingleton<ObsService>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<ModuleService>();
builder.Services.AddSingleton<GitHubService>();
builder.Services.AddDbContext<TownBulletinDbContext>(optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<TownBulletinDbContext>();
builder.Services.AddHttpClient();

WebApplication app = builder.Build();

// **********************************
//   Setting up singleton services.
// **********************************

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    PromptForFirewall();

using (IServiceScope scope = app.Services.CreateAsyncScope())
{
    TownBulletinDbContext townBulletinDbContext = scope.ServiceProvider.GetRequiredService<TownBulletinDbContext>();
    townBulletinDbContext.Database.Migrate();
    townBulletinDbContext.Settings.Populate();

    TwitchService twitchService = scope.ServiceProvider.GetRequiredService<TwitchService>();
    if (await twitchService.Initialize() && twitchService.IsLoggedIn)
        await twitchService.GetChannelInformation();

    TwitchChatService twitchChatService = scope.ServiceProvider.GetRequiredService<TwitchChatService>();
    if (await twitchChatService.Initialize() && twitchChatService.IsLoggedIn)
        await twitchChatService.GetChannelInformation();
        
    if (townBulletinDbContext.Settings.GetSetting("Autostart").Equals("true", StringComparison.InvariantCultureIgnoreCase))
    {
        List<Action> actions = new();

        if (twitchService.IsLoggedIn)
            actions.Add(async () => await twitchService.StartAsync());

        if (twitchChatService.IsLoggedIn)
            actions.Add(async () => await twitchChatService.StartAsync());

        ObsService obsService = scope.ServiceProvider.GetRequiredService<ObsService>();
        actions.Add(async () => await obsService.StartAsync());

        // TODO: Create discord service.

        Parallel.Invoke(actions.ToArray());
    }
}

// ******************************************
//   Configuring the HTTP request pipeline.
// ******************************************
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseTwitchLibEventSubWebhooks();

app.UseMiddleware<IPAccessListMiddleware>(configuration["IPAccessList"]);

app.UseStaticFiles();

app.UseRouting();


app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

static void PromptForFirewall()
{
    IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
    IPEndPoint ipLocalEndPoint = new(ipAddress, 443);

    TcpListener t = new(ipLocalEndPoint);
    t.Start();
    t.Stop();
}

static void CreateCertificate(string path, string password)
{
    if (File.Exists(path))
    {
        Console.WriteLine("Certificate file already exists, new certificate not generated.");
        return;
    }

    SubjectAlternativeNameBuilder sanBuilder = new();
    sanBuilder.AddIpAddress(IPAddress.Loopback);
    sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
    sanBuilder.AddDnsName("localhost");
    sanBuilder.AddDnsName(Environment.MachineName);

    X500DistinguishedName distinguishedName = new($"CN=localhost");

    using RSA rsa = RSA.Create(2048);
    CertificateRequest request = new(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

    request.CertificateExtensions.Add(
        new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));

    request.CertificateExtensions.Add(
        new X509EnhancedKeyUsageExtension(
            new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

    request.CertificateExtensions.Add(sanBuilder.Build());

    X509Certificate2 certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
    certificate.FriendlyName = "localhost";

    File.WriteAllBytes(path, certificate.Export(X509ContentType.Pfx, password));
}