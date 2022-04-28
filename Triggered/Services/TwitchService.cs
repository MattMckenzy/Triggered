using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.PubSub;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service abstracted from <see cref="TwitchServiceBase"/> that handles connections to, registers events for and exposes <see cref="TwitchLib.PubSub.TwitchPubSub"/> and <see cref="ITwitchEventSubWebhooks"/>.
    /// </summary>
    public class TwitchService : TwitchServiceBase
    {
        #region Private Variables

        private MessagingService MessagingService { get; }
        private ModuleService ModuleService { get; }
        private IConfiguration Configuration { get; }
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }

        private IEnumerable<(string, string, Dictionary<string, string>, string)> EventSubscriptions { get; } = new List<(string, string, Dictionary<string, string>, string)>
        {
            ("channel.update", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.follow", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.subscribe", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.subscription.end", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.subscription.gift", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.subscription.message", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.cheer", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.raid", "1", new Dictionary<string, string> { { "to_broadcaster_user_id", "" } }, "webhook"),
            ("channel.raid", "1", new Dictionary<string, string> { { "from_broadcaster_user_id", "" } }, "webhook"),
            ("channel.ban", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.unban", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.moderator.add", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.moderator.remove", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.channel_points_custom_reward.add", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.channel_points_custom_reward.update", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.channel_points_custom_reward.remove", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.channel_points_custom_reward_redemption.add", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.channel_points_custom_reward_redemption.update", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.poll.begin", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.poll.progress", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.poll.end", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.prediction.begin", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.prediction.progress", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.prediction.lock", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.prediction.end", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            //("drop.entitlement.grant", "1", new Dictionary<string, string> { { "organization_id", "" } }, "webhook"),
            //("extension.bits_transaction.create", "1", new Dictionary<string, string> { { "extension_client_id", "" } }, "webhook"),
            ("channel.goal.begin", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.goal.progress", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.goal.end", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.hype_train.begin", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.hype_train.progress", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("channel.hype_train.end", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("stream.online", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("stream.offline", "1", new Dictionary<string, string> { { "broadcaster_user_id", "" } }, "webhook"),
            ("user.authorization.grant", "1", new Dictionary<string, string> { { "client_id", "" } }, "webhook"),
            ("user.authorization.revoke", "1", new Dictionary<string, string> { { "client_id", "" } }, "webhook"),
            ("user.update", "1", new Dictionary<string, string> { { "user_id", "" } }, "webhook"),
        };

        #endregion

        #region Public Properties

        /// <summary>
        /// Class representing interactions with the Twitch PubSub. Please see the TwitchLib PubSub documentation here: https://swiftyspiffy.com/TwitchLib/PubSub/index.html
        /// </summary>
        public TwitchPubSub TwitchPubSub { get; private set; } = new();

        /// <summary>
        /// Class representing interactions with the Twitch EventSub. Please see the TwitchLib EventSub GitHub page here: https://github.com/TwitchLib/TwitchLib.EventSub.Webhooks
        /// </summary>
        public ITwitchEventSubWebhooks TwitchEventSubWebhooks { get; private set; } = null!;

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>       
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="moduleService">Injected <see cref="Services.ModuleService"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        /// <param name="encryptionService">Injected <see cref="EncryptionService"/>.</param>
        /// <param name="eventSubWebhooks">Injected <see cref="ITwitchEventSubWebhooks"/>.</param>
        /// <param name="configuration">Injected <see cref="IConfiguration"/>.</param>
        public TwitchService(IDbContextFactory<TriggeredDbContext> dbContextFactory,
                                ModuleService moduleService,
                                MessagingService messagingService,
                                EncryptionService encryptionService,
                                ITwitchEventSubWebhooks eventSubWebhooks,
                                IConfiguration configuration)
            : base(dbContextFactory, messagingService, encryptionService)
        {
            MessagingService = messagingService;
            ModuleService = moduleService;
            Configuration = configuration;
            DbContextFactory = dbContextFactory;
            TwitchEventSubWebhooks = eventSubWebhooks;

            ConnectFunction = Connect;
            DisconnectFunction = Disconnect;

            Scopes.AddRange(new AuthScopes[] {
                AuthScopes.AnalyticsReadExtensions,
                AuthScopes.AnalyticsReadGames,
                AuthScopes.BitsRead,
                AuthScopes.UserEdit,
                AuthScopes.ChannelEditCommercial,
                AuthScopes.ChannelManageBroadcast,
                AuthScopes.ChannelManageExtensions,
                AuthScopes.ChannelManagePolls,
                AuthScopes.ChannelManagePredictions,
                AuthScopes.ChannelManageRedemptions,
                AuthScopes.ChannelManageSchedule,
                AuthScopes.ChannelManageVideos,
                AuthScopes.ChannelReadEditors,
                AuthScopes.ChannelReadGoals,
                AuthScopes.ChannelReadHypeTrain,
                AuthScopes.ChannelReadPolls,
                AuthScopes.ChannelReadPredictions,
                AuthScopes.ChannelReadRedemptions,
                AuthScopes.ChannelReadStreamKey,
                AuthScopes.ChannelReadSubscriptions,
                AuthScopes.ClipsEdit,
                AuthScopes.ModerationRead,
                AuthScopes.ModeratorManageBannedUsers,
                AuthScopes.ModeratorReadBlockedTerms,
                AuthScopes.ModeratorManageBlockedTerms,
                AuthScopes.ModeratorManageAutomod,
                AuthScopes.ModeratorReadAutomodSettings,
                AuthScopes.ModeratorManageAutomodSettings,
                AuthScopes.ModeratorReadChatSettings,
                AuthScopes.ModeratorManageChatSettings,
                AuthScopes.UserManageBlockedUsers,
                AuthScopes.UserReadBlockedUsers,
                AuthScopes.UserReadBroadcast,
                AuthScopes.UserReadEmail,
                AuthScopes.UserReadFollows,
                AuthScopes.UserReadSubscriptions,
                AuthScopes.ChannelModerate,
                AuthScopes.ChatEdit,
                AuthScopes.ChatRead,
                AuthScopes.WhispersEdit,
                AuthScopes.WhispersRead
            });

            ModuleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(TwitchService), typeof(TwitchService), this)
            });

            ModuleService.InitializeSupportedEventsAndParameters(TwitchPubSub);
            ModuleService.InitializeSupportedEventsAndParameters(TwitchEventSubWebhooks);
        }

        #endregion

        #region Service Connections

        protected async Task Connect()
        {
            await InitializePubSub();

            await InitializeEventSub();
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;

        protected async Task Disconnect()
        {
            await ModuleService.DeregisterEvents(TwitchPubSub);
            await ModuleService.DeregisterEvents(TwitchEventSubWebhooks);
            TwitchPubSub.Disconnect();
        }

        #endregion

        #region Service Events


        #endregion

        #region Private Helpers

        private async Task InitializePubSub()
        {
            if (User == null)
            {
                await MessagingService.AddMessage("Could not start Twitch PubSub. User information was not found.", MessageCategory.Service, LogLevel.Error);
                return;
            }

            await ModuleService.RegisterEvents(TwitchPubSub);
            await ModuleService.RegisterEvents(TwitchEventSubWebhooks);

            TwitchPubSub.ListenToAutomodQueue(User.Id, User.Id);
            TwitchPubSub.ListenToChatModeratorActions(User.Id, User.Id);
            TwitchPubSub.ListenToUserModerationNotifications(User.Id, User.Id);
            TwitchPubSub.ListenToBitsEventsV2(User.Id);
            TwitchPubSub.ListenToChannelPoints(User.Id);
            TwitchPubSub.ListenToFollows(User.Id);
            TwitchPubSub.ListenToLeaderboards(User.Id);
            TwitchPubSub.ListenToPredictions(User.Id);
            TwitchPubSub.ListenToRaid(User.Id);
            TwitchPubSub.ListenToSubscriptions(User.Id);
            TwitchPubSub.ListenToVideoPlayback(User.Id);
            TwitchPubSub.ListenToWhispers(User.Id);

            TwitchPubSub.OnPubSubServiceConnected += async (_, __) =>
            {
                TwitchPubSub.SendTopics(await GetValidToken());
                await MessagingService.AddMessage("Twitch PubSub connected!", MessageCategory.Service, LogLevel.Debug);
            };

            TwitchPubSub.OnListenResponse += async (_, eventArgs) =>
            {
                if (eventArgs.Successful)
                    await MessagingService.AddMessage($"Twitch PubSub succesfully listening for topic: {eventArgs.Topic}", MessageCategory.Service, LogLevel.Trace);
                else if (!eventArgs.Successful)
                    await MessagingService.AddMessage($"Twitch PubSub failed to listen for topic: {eventArgs.Topic}", MessageCategory.Service, LogLevel.Trace);
            };

            TwitchPubSub.OnPubSubServiceClosed += async (_, __) =>
            {
                if (!_cancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
                {
                    _cancellationTokenSource.Cancel();
                    await MessagingService.AddMessage("Could not connect to Twitch PubSub service after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
                }
                else if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    disconnections++;
                    await MessagingService.AddMessage($"Disconnected from Twitch PubSub. Connection retrying...", MessageCategory.Service, LogLevel.Warning);
                    TwitchPubSub.Connect();
                }

                lastDisconnection = DateTime.Now;
            };

            TwitchPubSub.Connect();
        }


        private async Task InitializeEventSub()
        {
            // TODO: Add Extension EBS.

            TriggeredDbContext triggeredDbContext = DbContextFactory.CreateDbContext();

            string serverAccessToken = await TwitchAPI.Auth.GetServerAccessToken();

            string webhookHost = triggeredDbContext.Settings.GetSetting("WebhookHost");
            string secret = Configuration["TwitchSecret"];

            if (bool.TryParse(triggeredDbContext.Settings.GetSetting("UseWebhookHostProxy"), out bool result) && result)
            {
                HubConnection hubConnection;
                try
                {
                    hubConnection = new HubConnectionBuilder()
                       .WithUrl($"{webhookHost}/proxyhub")
                       .WithAutomaticReconnect()
                       .Build();

                    await hubConnection.StartAsync(_cancellationTokenSource.Token);
                    secret = await hubConnection.InvokeAsync<string>("GetSecret", 89, _cancellationTokenSource.Token);
                    await hubConnection.StopAsync(_cancellationTokenSource.Token);

                    hubConnection = new HubConnectionBuilder()
                       .WithUrl($"{webhookHost}/proxyhub?secret={secret}")
                       .WithAutomaticReconnect()
                       .Build();

                    await hubConnection.StartAsync(_cancellationTokenSource.Token);
                }
                catch (Exception exception)
                {
                    await MessagingService.AddMessage($"Could not connect to EventSub proxy: {exception.Message}", MessageCategory.Service, LogLevel.Error);
                    _cancellationTokenSource.Cancel();
                    return;
                }

                hubConnection.Closed += async (exception) =>
                {
                    await MessagingService.AddMessage($"EventSub proxy closed: {exception?.Message ?? "N/A"}", MessageCategory.Service, LogLevel.Error);
                    _cancellationTokenSource.Cancel();
                };
                hubConnection.Reconnecting += async (exception) => await MessagingService.AddMessage($"EventSub proxy reconnecting: {exception?.Message ?? "N/A"}", MessageCategory.Service, LogLevel.Debug);
                hubConnection.Reconnected += async (_) => await MessagingService.AddMessage($"Succesfully reconnected to EventSub proxy!", MessageCategory.Service, LogLevel.Debug);

                await MessagingService.AddMessage("Connected to EventSub proxy!", MessageCategory.Service, LogLevel.Debug);

                hubConnection.On<Dictionary<string, string>?, string>(nameof(ProcessEventNotification), ProcessEventNotification);
            }

            if (!_cancellationTokenSource.IsCancellationRequested)
                await RefreshEventSubscriptions($"{webhookHost}/eventsub/webhook", serverAccessToken, secret);
        }

        private async Task RefreshEventSubscriptions(string webhook, string serverAccessToken, string secret)
        {            
            if (!await DeleteEventSubscriptions(serverAccessToken))
            {
                _cancellationTokenSource.Cancel();
                await MessagingService.AddMessage($"Failed to clear existing Twitch EventSub webhooks, service stopped.", MessageCategory.Service, LogLevel.Error);
            }

            List<Task<bool>> eventSubscriptionTasks = new();
            foreach((string type, string version, Dictionary<string, string> conditions, string method) in EventSubscriptions)
            {
                eventSubscriptionTasks.Add(CreateEventSubscription(webhook, serverAccessToken, secret, type, version, conditions, method));
            }

            IEnumerable<bool> eventSubscriptionTaskSuccesses = await Task.WhenAll(eventSubscriptionTasks);

            if (eventSubscriptionTaskSuccesses.Contains(false))
            {
                _cancellationTokenSource.Cancel();
                await MessagingService.AddMessage($"Could not register all Twitch EventSub webhooks, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else
                await MessagingService.AddMessage($"Succesfully registered all Twitch EventSub webhooks!", MessageCategory.Service, LogLevel.Debug);     
        }


        private async Task<bool> DeleteEventSubscriptions(string serverAccessToken)
        {
            GetEventSubSubscriptionsResponse? eventSubscriptions;
            do
            {
                eventSubscriptions = await TwitchAPI.Helix.EventSub.GetEventSubSubscriptionsAsync(accessToken: serverAccessToken);

                if (eventSubscriptions == null)
                {
                    await MessagingService.AddMessage($"Failed to retrieve event subscription information from Twitch.", MessageCategory.Service, LogLevel.Error);
                    return false;
                }
                else
                {
                    foreach (EventSubSubscription eventSubscription in eventSubscriptions.Subscriptions)
                    {
                        await TwitchAPI.Helix.EventSub.DeleteEventSubSubscriptionAsync(eventSubscription.Id, accessToken: serverAccessToken);
                    }
                }


            } while (eventSubscriptions.Subscriptions.Length > 0);

            return true;
        }

        private async Task<bool> CreateEventSubscription(string webhook, string serverAccessToken, string secret, string type, string version, Dictionary<string, string> conditions, string method)
        {
            //TODO: add drop and extension events. 
            if (conditions.ContainsKey("broadcaster_user_id"))
                conditions["broadcaster_user_id"] = User!.Id;
            if (conditions.ContainsKey("from_broadcaster_user_id"))
                conditions["from_broadcaster_user_id"] = User!.Id;
            if (conditions.ContainsKey("to_broadcaster_user_id"))
                conditions["to_broadcaster_user_id"] = User!.Id;
            if (conditions.ContainsKey("user_id"))
                conditions["user_id"] = User!.Id;
            if (conditions.ContainsKey("client_id"))
                conditions["client_id"] = await TwitchAPI.Settings.GetClientIdAsync();

            CreateEventSubSubscriptionResponse? createEventSubSubscriptionResponse =
                await TwitchAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(type, version, conditions, method, webhook, secret, accessToken: serverAccessToken);

            if (createEventSubSubscriptionResponse?.Subscriptions.FirstOrDefault() == null)
                await MessagingService.AddMessage($"Failed to register webhook \"{type}\" at \"{webhook}\".", MessageCategory.Service, LogLevel.Error);
            else if (createEventSubSubscriptionResponse.Subscriptions.FirstOrDefault()!.Status != "enabled" && createEventSubSubscriptionResponse.Subscriptions.FirstOrDefault()!.Status != "webhook_callback_verification_pending")
                await MessagingService.AddMessage($"Failed to register webhook \"{type}\" at \"{webhook}\" because \"{createEventSubSubscriptionResponse.Subscriptions.FirstOrDefault()!.Status}\".", MessageCategory.Service, LogLevel.Error);

            DateTime verificationTime = DateTime.Now + TimeSpan.FromMinutes(1);
            TimeSpan delayTime = TimeSpan.FromSeconds(1);
            while (verificationTime > DateTime.Now)
            {
                await Task.Delay(delayTime);
                delayTime = delayTime.Multiply(1.5);

                GetEventSubSubscriptionsResponse? eventSubscriptions =
                    await TwitchAPI.Helix.EventSub.GetEventSubSubscriptionsAsync(type: type, accessToken: serverAccessToken);

                if (eventSubscriptions?.Subscriptions?.FirstOrDefault()?.Status == "enabled")
                {
                    await MessagingService.AddMessage($"Twitch succesfully subscribed to webhook for topic: \"{type}\"", MessageCategory.Service, LogLevel.Trace);
                    return true;
                }
                else if (eventSubscriptions?.Subscriptions?.FirstOrDefault()?.Status == "webhook_callback_verification_pending")
                {
                    await MessagingService.AddMessage($"Twitch still waiting for verification on topic: \"{type}\"", MessageCategory.Service, LogLevel.Trace);
                    continue;
                }
                else
                {
                    await MessagingService.AddMessage($"Twitch failed to verify subscription to webhook for topic: \"{type}\" with status: \"{eventSubscriptions?.Subscriptions?.FirstOrDefault()?.Status}\"", MessageCategory.Service, LogLevel.Trace);
                    return false;
                }
            }

            await MessagingService.AddMessage($"Twitch webhook verification for topic: \"{type}\" timed out.", MessageCategory.Service, LogLevel.Error);
            return false;
        }

        public async Task ProcessEventNotification(Dictionary<string, string>? headers, string body)
        {
            using MemoryStream stream = new();
            using StreamWriter streamWriter = new(stream);
            await streamWriter.WriteAsync(body);
            await streamWriter.FlushAsync();
            stream.Position = 0;

            await TwitchEventSubWebhooks.ProcessNotificationAsync(headers ?? new Dictionary<string, string>(), stream);
        }

        #endregion
    }
}
