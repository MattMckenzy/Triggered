using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
builder.Services.AddSingleton<TwitchBotService>();
builder.Services.AddSingleton<ObsService>();
builder.Services.AddSingleton<EncryptionService>();
builder.Services.AddSingleton<ModuleService>();
builder.Services.AddDbContext<TownBulletinDbContext>(optionsLifetime: ServiceLifetime.Singleton);
builder.Services.AddDbContextFactory<TownBulletinDbContext>();

WebApplication app = builder.Build();

// **********************************
//   Setting up singleton services.
// **********************************

PromptForFirewall();

using (IServiceScope scope = app.Services.CreateAsyncScope())
{
    TownBulletinDbContext townBulletinDbContext = scope.ServiceProvider.GetRequiredService<TownBulletinDbContext>();
    townBulletinDbContext.Database.Migrate();
    townBulletinDbContext.Settings.Populate();

    TwitchService twitchService = scope.ServiceProvider.GetRequiredService<TwitchService>();
    if (await twitchService.Initialize() && twitchService.IsLoggedIn)
        await twitchService.GetChannelInformation();

    TwitchBotService twitchBotService = scope.ServiceProvider.GetRequiredService<TwitchBotService>();
    if (await twitchBotService.Initialize() && twitchBotService.IsLoggedIn)
        await twitchBotService.GetChannelInformation();
        
    if (townBulletinDbContext.Settings.GetSetting("Autostart").Equals("true", StringComparison.InvariantCultureIgnoreCase))
    {
        List<Action> actions = new();

        if (twitchService.IsLoggedIn)
            actions.Add(async () => await twitchService.StartAsync());

        if (twitchBotService.IsLoggedIn)
            actions.Add(async () => await twitchBotService.StartAsync());

        ObsService obsService = scope.ServiceProvider.GetRequiredService<ObsService>();
        actions.Add(async () => await obsService.StartAsync());

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
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        IPAddress ipAddress = Dns.GetHostEntry(Dns.GetHostName()).AddressList[0];
        IPEndPoint ipLocalEndPoint = new(ipAddress, 443);

        TcpListener t = new(ipLocalEndPoint);
        t.Start();
        t.Stop();
    }
}