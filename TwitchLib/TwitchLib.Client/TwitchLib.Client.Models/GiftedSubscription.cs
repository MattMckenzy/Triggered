using System;
using System.Collections.Generic;

using TwitchLib.Client.Enums;
using TwitchLib.Client.Models.Internal;

namespace TwitchLib.Client.Models
{
    public class GiftedSubscription
    {
        private const string AnonymousGifterUserId = "274598607";

        public List<KeyValuePair<string, string>> Badges { get; set; }

        public List<KeyValuePair<string, string>> BadgeInfo { get; set; }

        public string Color { get; set; }

        public string DisplayName { get; set; }

        public string Emotes { get; set; }

        public string Id { get; set; }

        public bool IsModerator { get; set; }

        public bool IsSubscriber { get; set; }

        public bool IsTurbo { get; set; }

        public bool IsAnonymous { get; set; }

        public string Login { get; set; }

        public string MsgId { get; set; }

        public string MsgParamMonths { get; set; }

        public string MsgParamRecipientDisplayName { get; set; }

        public string MsgParamRecipientId { get; set; }

        public string MsgParamRecipientUserName { get; set; }

        public string MsgParamSubPlanName { get; set; }

        public SubscriptionPlan MsgParamSubPlan { get; set; }

        public string RoomId { get; set; }

        public string SystemMsg { get; set; }

        public string SystemMsgParsed { get; set; }

        public string TmiSentTs { get; set; }

        public string UserId { get; set; }

        public UserType UserType { get; set; }

        public string MsgParamMultiMonthGiftDuration { get; set; }

        public GiftedSubscription(IrcMessage ircMessage)
        {
            foreach (var tag in ircMessage.Tags.Keys)
            {
                var tagValue = ircMessage.Tags[tag];

                switch (tag)
                {
                    case Tags.Badges:
                        Badges = Common.Helpers.ParseBadges(tagValue);
                        break;
                    case Tags.BadgeInfo:
                        BadgeInfo = Common.Helpers.ParseBadges(tagValue);
                        break;
                    case Tags.Color:
                        Color = tagValue;
                        break;
                    case Tags.DisplayName:
                        DisplayName = tagValue;
                        break;
                    case Tags.Emotes:
                        Emotes = tagValue;
                        break;
                    case Tags.Id:
                        Id = tagValue;
                        break;
                    case Tags.Login:
                        Login = tagValue;
                        break;
                    case Tags.Mod:
                        IsModerator = Common.Helpers.ConvertToBool(tagValue);
                        break;
                    case Tags.MsgId:
                        MsgId = tagValue;
                        break;
                    case Tags.MsgParamMonths:
                        MsgParamMonths = tagValue;
                        break;
                    case Tags.MsgParamRecipientDisplayname:
                        MsgParamRecipientDisplayName = tagValue;
                        break;
                    case Tags.MsgParamRecipientId:
                        MsgParamRecipientId = tagValue;
                        break;
                    case Tags.MsgParamRecipientUsername:
                        MsgParamRecipientUserName = tagValue;
                        break;
                    case Tags.MsgParamSubPlanName:
                        MsgParamSubPlanName = tagValue;
                        break;
                    case Tags.MsgParamSubPlan:
                        switch (tagValue)
                        {
                            case "prime":
                                MsgParamSubPlan = SubscriptionPlan.Prime;
                                break;
                            case "1000":
                                MsgParamSubPlan = SubscriptionPlan.Tier1;
                                break;
                            case "2000":
                                MsgParamSubPlan = SubscriptionPlan.Tier2;
                                break;
                            case "3000":
                                MsgParamSubPlan = SubscriptionPlan.Tier3;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(nameof(tagValue.ToLower));
                        }
                        break;
                    case Tags.RoomId:
                        RoomId = tagValue;
                        break;
                    case Tags.Subscriber:
                        IsSubscriber = Common.Helpers.ConvertToBool(tagValue);
                        break;
                    case Tags.SystemMsg:
                        SystemMsg = tagValue;
                        SystemMsgParsed = tagValue.Replace("\\s", " ").Replace("\\n", "");
                        break;
                    case Tags.TmiSentTs:
                        TmiSentTs = tagValue;
                        break;
                    case Tags.Turbo:
                        IsTurbo = Common.Helpers.ConvertToBool(tagValue);
                        break;
                    case Tags.UserId:
                        UserId = tagValue;
                        if (UserId == AnonymousGifterUserId)
                        {
                            IsAnonymous = true;
                        }
                        break;
                    case Tags.UserType:
                        switch (tagValue)
                        {
                            case "mod":
                                UserType = UserType.Moderator;
                                break;
                            case "global_mod":
                                UserType = UserType.GlobalModerator;
                                break;
                            case "admin":
                                UserType = UserType.Admin;
                                break;
                            case "staff":
                                UserType = UserType.Staff;
                                break;
                            default:
                                UserType = UserType.Viewer;
                                break;
                        }
                        break;
                    case Tags.MsgParamMultiMonthGiftDuration:
                        MsgParamMultiMonthGiftDuration = tagValue;
                        break;
                }
            }
        }

        public GiftedSubscription(
            List<KeyValuePair<string, string>> badges,
            List<KeyValuePair<string, string>> badgeInfo,
            string color,
            string displayName,
            string emotes,
            string id,
            string login,
            bool isModerator,
            string msgId,
            string msgParamMonths,
            string msgParamRecipientDisplayName,
            string msgParamRecipientId,
            string msgParamRecipientUserName,
            string msgParamSubPlanName,
            string msgMultiMonthDuration,
            SubscriptionPlan msgParamSubPlan,
            string roomId,
            bool isSubscriber,
            string systemMsg,
            string systemMsgParsed,
            string tmiSentTs,
            bool isTurbo,
            UserType userType,
            string userId)
        {
            Badges = badges;
            BadgeInfo = badgeInfo;
            Color = color;
            DisplayName = displayName;
            Emotes = emotes;
            Id = id;
            Login = login;
            IsModerator = isModerator;
            MsgId = msgId;
            MsgParamMonths = msgParamMonths;
            MsgParamRecipientDisplayName = msgParamRecipientDisplayName;
            MsgParamRecipientId = msgParamRecipientId;
            MsgParamRecipientUserName = msgParamRecipientUserName;
            MsgParamSubPlanName = msgParamSubPlanName;
            MsgParamSubPlan = msgParamSubPlan;
            MsgParamMultiMonthGiftDuration = msgMultiMonthDuration;
            RoomId = roomId;
            IsSubscriber = isSubscriber;
            SystemMsg = systemMsg;
            SystemMsgParsed = systemMsgParsed;
            TmiSentTs = tmiSentTs;
            IsTurbo = isTurbo;
            UserType = userType;
            UserId = userId;
        }

        public GiftedSubscription()
        {
        }
    }
}
