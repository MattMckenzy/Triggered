using System.Collections.Generic;

using TwitchLib.Client.Enums;
using TwitchLib.Client.Models.Internal;

namespace TwitchLib.Client.Models
{
    public class RaidNotification
    {
        public List<KeyValuePair<string, string>> Badges { get; set; }

        public List<KeyValuePair<string, string>> BadgeInfo { get; set; }

        public string Color { get; set; }

        public string DisplayName { get; set; }

        public string Emotes { get; set; }

        public string Id { get; set; }

        public string Login { get; set; }

        public bool Moderator { get; set; }

        public string MsgId { get; set; }

        public string MsgParamDisplayName { get; set; }

        public string MsgParamLogin { get; set; }

        public string MsgParamViewerCount { get; set; }

        public string RoomId { get; set; }

        public bool Subscriber { get; set; }

        public string SystemMsg { get; set; }

        public string SystemMsgParsed { get; set; }

        public string TmiSentTs { get; set; }

        public bool Turbo { get; set; }

        public string UserId { get; set; }

        public UserType UserType { get; set; }

        // @badges=;color=#FF0000;display-name=Heinki;emotes=;id=4fb7ab2d-aa2c-4886-a286-46e20443f3d6;login=heinki;mod=0;msg-id=raid;msg-param-displayName=Heinki;msg-param-login=heinki;msg-param-viewerCount=4;room-id=27229958;subscriber=0;system-msg=4\sraiders\sfrom\sHeinki\shave\sjoined\n!;tmi-sent-ts=1510249711023;turbo=0;user-id=44110799;user-type= :tmi.twitch.tv USERNOTICE #pandablack
        public RaidNotification(IrcMessage ircMessage)
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
                    case Tags.Login:
                        Login = tagValue;
                        break;
                    case Tags.Mod:
                        Moderator = Common.Helpers.ConvertToBool(tagValue);
                        break;
                    case Tags.MsgId:
                        MsgId = tagValue;
                        break;
                    case Tags.MsgParamDisplayname:
                        MsgParamDisplayName = tagValue;
                        break;
                    case Tags.MsgParamLogin:
                        MsgParamLogin = tagValue;
                        break;
                    case Tags.MsgParamViewerCount:
                        MsgParamViewerCount = tagValue;
                        break;
                    case Tags.RoomId:
                        RoomId = tagValue;
                        break;
                    case Tags.Subscriber:
                        Subscriber = Common.Helpers.ConvertToBool(tagValue);
                        break;
                    case Tags.SystemMsg:
                        SystemMsg = tagValue;
                        SystemMsgParsed = tagValue.Replace("\\s", " ").Replace("\\n", "");
                        break;
                    case Tags.TmiSentTs:
                        TmiSentTs = tagValue;
                        break;
                    case Tags.Turbo:
                        Turbo = Common.Helpers.ConvertToBool(tagValue);
                        break;
                    case Tags.UserId:
                        UserId = tagValue;
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
                }
            }
        }

        public RaidNotification(
            List<KeyValuePair<string, string>> badges,
            List<KeyValuePair<string, string>> badgeInfo,
            string color,
            string displayName,
            string emotes,
            string id,
            string login,
            bool moderator,
            string msgId,
            string msgParamDisplayName,
            string msgParamLogin,
            string msgParamViewerCount,
            string roomId,
            bool subscriber,
            string systemMsg,
            string systemMsgParsed,
            string tmiSentTs,
            bool turbo,
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
            Moderator = moderator;
            MsgId = msgId;
            MsgParamDisplayName = msgParamDisplayName;
            MsgParamLogin = msgParamLogin;
            MsgParamViewerCount = msgParamViewerCount;
            RoomId = roomId;
            Subscriber = subscriber;
            SystemMsg = systemMsg;
            SystemMsgParsed = systemMsgParsed;
            TmiSentTs = tmiSentTs;
            Turbo = turbo;
            UserType = userType;
            UserId = userId;
        }

        public RaidNotification()
        {
        }
    }
}

