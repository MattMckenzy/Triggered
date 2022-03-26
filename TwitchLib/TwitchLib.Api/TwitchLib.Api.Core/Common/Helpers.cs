using System;
using TwitchLib.Api.Core.Enums;

namespace TwitchLib.Api.Core.Common
{
    /// <summary>Static class of helper functions used around the project.</summary>
    public static class Helpers
    {
        /// <summary>
        /// Function that extracts just the token for consistency
        /// </summary>
        /// <param name="token">Full token string</param>
        /// <returns></returns>
        public static string FormatOAuth(string token)
        {
            return token.Contains(" ") ? token.Split(' ')[1] : token;
        }

        public static string AuthScopesToString(AuthScopes scope)
        {
            switch (scope)
            {
                // Api scopes
                case AuthScopes.AnalyticsReadExtensions:
                    return "analytics:read:extensions";
                case AuthScopes.AnalyticsReadGames:
					return "analytics:read:games";
                case AuthScopes.BitsRead:
					return "bits:read";
                case AuthScopes.UserEdit:
					return "user:edit";
                case AuthScopes.ChannelEditCommercial:
					return "channel:edit:commercial";
                case AuthScopes.ChannelManageBroadcast:
					return "channel:manage:broadcast";
                case AuthScopes.ChannelManageExtensions:
					return "channel:manage:extensions";
                case AuthScopes.ChannelManagePolls:
					return "channel:manage:polls";
                case AuthScopes.ChannelManagePredictions:
					return "channel:manage:predictions";
                case AuthScopes.ChannelManageRedemptions:
					return "channel:manage:redemptions";
                case AuthScopes.ChannelManageSchedule:
					return "channel:manage:schedule";
                case AuthScopes.ChannelManageVideos:
					return "channel:manage:videos";
                case AuthScopes.ChannelReadEditors:
					return "channel:read:editors";
                case AuthScopes.ChannelReadGoals:
					return "channel:read:goals";
                case AuthScopes.ChannelReadHypeTrain:
					return "channel:read:hype_train";
                case AuthScopes.ChannelReadPolls:
					return "channel:read:polls";
                case AuthScopes.ChannelReadPredictions:
					return "channel:read:predictions";
                case AuthScopes.ChannelReadRedemptions:
					return "channel:read:redemptions";
                case AuthScopes.ChannelReadStreamKey:
					return "channel:read:stream_key";
                case AuthScopes.ChannelReadSubscriptions:
					return "channel:read:subscriptions";
                case AuthScopes.ClipsEdit:
					return "clips:edit";
                case AuthScopes.ModerationRead:
					return "moderation:read";
                case AuthScopes.ModeratorManageBannedUsers:
					return "moderator:manage:banned_users";
                case AuthScopes.ModeratorReadBlockedTerms:
					return "moderator:read:blocked_terms";
                case AuthScopes.ModeratorManageBlockedTerms:
					return "moderator:manage:blocked_terms";
                case AuthScopes.ModeratorManageAutomod:
					return "moderator:manage:automod";
                case AuthScopes.ModeratorReadAutomodSettings:
					return "moderator:read:automod_settings";
                case AuthScopes.ModeratorManageAutomodSettings:
					return "moderator:manage:automod_settings";
                case AuthScopes.ModeratorReadChatSettings:
					return "moderator:read:chat_settings";
                case AuthScopes.ModeratorManageChatSettings:
					return "moderator:manage:chat_settings";
                case AuthScopes.UserManageBlockedUsers:
					return "user:manage:blocked_users";
                case AuthScopes.UserReadBlockedUsers:
					return "user:read:blocked_users";
                case AuthScopes.UserReadBroadcast:
					return "user:read:broadcast";
                case AuthScopes.UserReadEmail:
					return "user:read:email";
                case AuthScopes.UserReadFollows:
					return "user:read:subscriptions";
                case AuthScopes.UserReadSubscriptions:
					return "user:read:follows";

                // Chat and PubSub scopes.
                case AuthScopes.ChannelModerate:
					return "channel:moderate";
                case AuthScopes.ChatEdit:
					return "chat:edit";
                case AuthScopes.ChatRead:
					return "chat:read";
                case AuthScopes.WhispersEdit:
					return "whispers:edit";
                case AuthScopes.WhispersRead:
					return "whispers:read";

                // Other.
                default:
                    return "";
            }
        }

        public static AuthScopes StringToAuthScopes(string scope)
        {
            switch (scope)
            {
                // Api scopes
                case "analytics:read:extensions":
                    return AuthScopes.AnalyticsReadExtensions;
                case "analytics:read:games":
                    return AuthScopes.AnalyticsReadGames;
                case "bits:read":
                    return AuthScopes.BitsRead;
                case "user:edit":
                    return AuthScopes.UserEdit;
                case "channel:edit:commercial":
                    return AuthScopes.ChannelEditCommercial;
                case "channel:manage:broadcast":
                    return AuthScopes.ChannelManageBroadcast;
                case "channel:manage:extensions":
                    return AuthScopes.ChannelManageExtensions;
                case "channel:manage:polls":
                    return AuthScopes.ChannelManagePolls;
                case "channel:manage:predictions":
                    return AuthScopes.ChannelManagePredictions;
                case "channel:manage:redemptions":
                    return AuthScopes.ChannelManageRedemptions;
                case "channel:manage:schedule":
                    return AuthScopes.ChannelManageSchedule;
                case "channel:manage:videos":
                    return AuthScopes.ChannelManageVideos;
                case "channel:read:editors":
                    return AuthScopes.ChannelReadEditors;
                case "channel:read:goals":
                    return AuthScopes.ChannelReadGoals;
                case "channel:read:hype_train":
                    return AuthScopes.ChannelReadHypeTrain;
                case "channel:read:polls":
                    return AuthScopes.ChannelReadPolls;
                case "channel:read:predictions":
                    return AuthScopes.ChannelReadPredictions;
                case "channel:read:redemptions":
                    return AuthScopes.ChannelReadRedemptions;
                case "channel:read:stream_key":
                    return AuthScopes.ChannelReadStreamKey;
                case "channel:read:subscriptions":
                    return AuthScopes.ChannelReadSubscriptions;
                case "clips:edit":
                    return AuthScopes.ClipsEdit;
                case "moderation:read":
                    return AuthScopes.ModerationRead;
                case "moderator:manage:banned_users":
                    return AuthScopes.ModeratorManageBannedUsers;
                case "moderator:read:blocked_terms":
                    return AuthScopes.ModeratorReadBlockedTerms;
                case "moderator:manage:blocked_terms":
                    return AuthScopes.ModeratorManageBlockedTerms;
                case "moderator:manage:automod":
                    return AuthScopes.ModeratorManageAutomod;
                case "moderator:read:automod_settings":
                    return AuthScopes.ModeratorReadAutomodSettings;
                case "moderator:manage:automod_settings":
                    return AuthScopes.ModeratorManageAutomodSettings;
                case "moderator:read:chat_settings":
                    return AuthScopes.ModeratorReadChatSettings;
                case "moderator:manage:chat_settings":
                    return AuthScopes.ModeratorManageChatSettings;
                case "user:manage:blockedusers":
                    return AuthScopes.UserManageBlockedUsers;
                case "user:read:blocked_users":
                    return AuthScopes.UserReadBlockedUsers;
                case "user:read:broadcast":
                    return AuthScopes.UserReadBroadcast;
                case "user:read:email":
                    return AuthScopes.UserReadEmail;
                case "user:read:subscriptions":
                    return AuthScopes.UserReadFollows;
                case "user:read:follows":
                    return AuthScopes.UserReadSubscriptions;

                // Chat and PubSub scopes.
                case "channel:moderate":
                    return AuthScopes.ChannelModerate;
                case "chat:edit":
                    return AuthScopes.ChatEdit;
                case "chat:read":
                    return AuthScopes.ChatRead;
                case "whispers:edit":
                    return AuthScopes.WhispersEdit;
                case "whispers:read":
                    return AuthScopes.WhispersRead;

                // Other.
                case "":
                    return AuthScopes.None;
                default:
                    throw new Exception("Unknown scope");
            }
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }
    }
}