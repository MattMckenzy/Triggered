using Microsoft.EntityFrameworkCore;
using TownBulletin.Extensions;
using TownBulletin.Models;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.PubSub;

namespace TownBulletin.Services
{
    public class TwitchService : TwitchServiceBase
    {
        private readonly MessagingService _messagingService;
        private readonly ModuleService _moduleService;
        private readonly ITwitchEventSubWebhooks _eventSubWebhooks;
        private readonly IConfiguration _configuration;
        private readonly IDbContextFactory<TownBulletinDbContext> _dbContextFactory;


        private readonly IEnumerable<(string, string, Dictionary<string, string>, string)> _eventSubscriptions = new List<(string, string, Dictionary<string, string>, string)>
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

        public TwitchPubSub TwitchPubSub { get; set; } = new();

        public TwitchService(IDbContextFactory<TownBulletinDbContext> dbContextFactory,
                                ModuleService moduleService,
                                MessagingService messagingService,
                                EncryptionService encryptionService,
                                ITwitchEventSubWebhooks eventSubWebhooks,
                                IConfiguration configuration)
            : base(dbContextFactory, messagingService, encryptionService)
        {
            _messagingService = messagingService;
            _moduleService = moduleService;
            _eventSubWebhooks = eventSubWebhooks;
            _configuration = configuration;
            _dbContextFactory = dbContextFactory;

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

            _moduleService.RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(TwitchService), typeof(TwitchService), this),
                (nameof(ITwitchEventSubWebhooks), typeof(ITwitchEventSubWebhooks), this),
            });
            _moduleService.InitializeSupportedEventsAndParameters(TwitchPubSub);
        }

        protected async Task Connect()
        {
            string? accessToken = await GetValidToken();
            if (accessToken == null)
            {
                await _messagingService.AddMessage("Could not start Twitch services. Access Token was not found.", MessageCategory.Service, LogLevel.Error);
                return;
            }

            if (User == null)
            {
                await _messagingService.AddMessage("Could not start Twitch services. User information was not found.", MessageCategory.Service, LogLevel.Error);
                return;
            }

            _moduleService.RegisterEvents(TwitchPubSub);
            _moduleService.RegisterEvents(_eventSubWebhooks);

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

            TwitchPubSub.OnPubSubServiceConnected += TwitchPubSub_OnPubSubServiceConnected;
            TwitchPubSub.OnListenResponse += TwitchPubSub_OnListenResponse;
            TwitchPubSub.OnPubSubServiceClosed += TwitchPubSub_OnPubSubServiceClosed;

            TwitchPubSub.Connect();

            // TODO: Add Extension EBS.

            TownBulletinDbContext townBulletinDbContext = _dbContextFactory.CreateDbContext();

            string serverAccessToken = TwitchAPI.Auth.GetServerAccessToken();
            await RefreshEventSubscriptions($"{townBulletinDbContext.Settings.GetSetting("WebhookHost")}/twitch/events/webhook", serverAccessToken, _configuration["TwitchSecret"]);
        }

        protected Task Disconnect()
        {
            _moduleService.DeregisterEvents(TwitchPubSub);
            _moduleService.DeregisterEvents(_eventSubWebhooks);
            TwitchPubSub.Disconnect();
            return Task.CompletedTask;
        }

        private async void TwitchPubSub_OnPubSubServiceConnected(object? sender, EventArgs e)
        {
            TwitchPubSub.SendTopics(await GetValidToken());
            await _messagingService.AddMessage("Twitch PubSub connected!", MessageCategory.Service, LogLevel.Debug);
        }

        private int disconnections = 0;
        private DateTime lastDisconnection = DateTime.MinValue;
        private async void TwitchPubSub_OnPubSubServiceClosed(object? sender, EventArgs e)
        {
            if (!_cancellationTokenSource.IsCancellationRequested && DateTime.Now - lastDisconnection < TimeSpan.FromMinutes(1) && disconnections >= 3)
            {
                _cancellationTokenSource.Cancel();
                await _messagingService.AddMessage("Could not connect to Twitch PubSub service after three retries, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else if (!_cancellationTokenSource.IsCancellationRequested)
            {
                disconnections++;
                await _messagingService.AddMessage($"Disconnected from Twitch PubSub. Connection retrying...", MessageCategory.Service, LogLevel.Warning);
                TwitchPubSub.Connect();
            }

            lastDisconnection = DateTime.Now;
        }

        private async void TwitchPubSub_OnListenResponse(object? sender, TwitchLib.PubSub.Events.OnListenResponseArgs e)
        {
            if (e.Successful)
                await _messagingService.AddMessage($"Twitch PubSub succesfully listening for topic: {e.Topic}", MessageCategory.Service, LogLevel.Debug);
            else if (!e.Successful)
                await _messagingService.AddMessage($"Twitch PubSub failed to listen for topic: {e.Topic}", MessageCategory.Service, LogLevel.Error);
        }

        private async Task RefreshEventSubscriptions(string webhook, string serverAccessToken, string secret)
        {            
            if (!await DeleteEventSubscriptions(serverAccessToken))
            {
                _cancellationTokenSource.Cancel();
                await _messagingService.AddMessage($"Failed to clear existing Twitch EventSub webhooks, service stopped.", MessageCategory.Service, LogLevel.Error);
            }

            List<Task<bool>> eventSubscriptionTasks = new();
            foreach((string type, string version, Dictionary<string, string> conditions, string method) in _eventSubscriptions)
            {
                eventSubscriptionTasks.Add(CreateEventSubscription(webhook, serverAccessToken, secret, type, version, conditions, method));
            }

            IEnumerable<bool> eventSubscriptionTaskSuccesses = await Task.WhenAll(eventSubscriptionTasks);

            if (eventSubscriptionTaskSuccesses.Contains(false))
            {
                _cancellationTokenSource.Cancel();
                await _messagingService.AddMessage($"Could not register all Twitch EventSub webhooks, service stopped.", MessageCategory.Service, LogLevel.Error);
            }
            else
                await _messagingService.AddMessage($"Succesfully registered all Twitch EventSub webhooks!", MessageCategory.Service, LogLevel.Debug);     
        }


        private async Task<bool> DeleteEventSubscriptions(string serverAccessToken)
        {
            GetEventSubSubscriptionsResponse? eventSubscriptions;
            do
            {
                eventSubscriptions = await CallTwitchAPI(async () => await TwitchAPI.Helix.EventSub.GetEventSubSubscriptionsAsync(accessToken: serverAccessToken));

                if (eventSubscriptions == null)
                {
                    await _messagingService.AddMessage($"Failed to retrieve event subscription information from Twitch.", MessageCategory.Service, LogLevel.Error);
                    return false;
                }
                else
                {
                    foreach (EventSubSubscription eventSubscription in eventSubscriptions.Subscriptions)
                    {
                        await CallTwitchAPI(async () => await TwitchAPI.Helix.EventSub.DeleteEventSubSubscriptionAsync(eventSubscription.Id, accessToken: serverAccessToken));
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
                conditions["client_id"] = TwitchAPI.Settings.ClientId;

            CreateEventSubSubscriptionResponse? createEventSubSubscriptionResponse =
                await CallTwitchAPI(async () => await TwitchAPI.Helix.EventSub.CreateEventSubSubscriptionAsync(type, version, conditions, method, webhook, secret, accessToken: serverAccessToken));

            if (createEventSubSubscriptionResponse?.Subscriptions.FirstOrDefault() == null)
                await _messagingService.AddMessage($"Failed to register webhook \"{type}\" at \"{webhook}\".", MessageCategory.Service, LogLevel.Error);
            else if (createEventSubSubscriptionResponse.Subscriptions.FirstOrDefault()!.Status != "enabled" && createEventSubSubscriptionResponse.Subscriptions.FirstOrDefault()!.Status != "webhook_callback_verification_pending")
                await _messagingService.AddMessage($"Failed to register webhook \"{type}\" at \"{webhook}\" because \"{createEventSubSubscriptionResponse.Subscriptions.FirstOrDefault()!.Status}\".", MessageCategory.Service, LogLevel.Error);

            GetEventSubSubscriptionsResponse? eventSubscriptions  = null;
            DateTime verificationTime = DateTime.Now + TimeSpan.FromSeconds(18);
            while (verificationTime > DateTime.Now)
            {
                await Task.Delay(3000);

                eventSubscriptions =
                    await CallTwitchAPI(async () => await TwitchAPI.Helix.EventSub.GetEventSubSubscriptionsAsync(type: type, accessToken: serverAccessToken));

                if (eventSubscriptions?.Subscriptions?.FirstOrDefault()?.Status == "enabled")
                {
                    await _messagingService.AddMessage($"Twitch succesfully subscribed to webhook for topic: \"{type}\"", MessageCategory.Service, LogLevel.Debug);
                    return true;
                }
                else
                    continue;
            }

            await _messagingService.AddMessage($"Twitch failed to verify subscription to webhook for topic: \"{type}\" with status: \"{eventSubscriptions?.Subscriptions?.FirstOrDefault()?.Status}\"", MessageCategory.Service, LogLevel.Error);
            return false;
        }
    }
}
