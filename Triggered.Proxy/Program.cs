using Triggered.Proxy;
using Triggered.Proxy.EventSub.Webhooks.Middlewares;
using TwitchLib.EventSub.Webhooks;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.Models;

var builder = WebApplication.CreateBuilder(args);
IConfiguration configuration = builder.Configuration;

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(configuration.GetSection("Kestrel"), true);
});

builder.Services.AddSingleton<SecretManager>();
builder.Services.AddSingleton<ITwitchEventSubWebhooks, TwitchEventSubWebhooks>();
builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
}

app.UseRouting();

TwitchLibEventSubOptions options = new()
{
    EnableLogging = false,
    CallbackPath = "/eventsub/webhook"
};

app.UseWhen(context => context.Request.Method.Equals(HttpMethod.Post.Method, StringComparison.InvariantCultureIgnoreCase)
    && context.Request.Path.Equals(options.CallbackPath, StringComparison.InvariantCultureIgnoreCase), appBuilder =>
{
    appBuilder.UseMiddleware<EventSubSignatureVerificationMiddleware>();
    appBuilder.UseMiddleware<EventSubNotificationMiddleware>();
});

app.MapHub<ProxyHub>("/proxyhub");

app.Run();
