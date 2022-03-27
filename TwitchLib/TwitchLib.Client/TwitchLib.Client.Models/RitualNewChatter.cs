using System.Collections.Generic;

using TwitchLib.Client.Enums;
using TwitchLib.Client.Models.Internal;

namespace TwitchLib.Client.Models
{
    public class RitualNewChatter
    {
        public List<KeyValuePair<string, string>> Badges { get; set; }

        public List<KeyValuePair<string, string>> BadgeInfo { get; set; }

        public string Color { get; set; }

        public string DisplayName { get; set; }

        public string Emotes { get; set; }

        public string Id { get; set; }

        public bool IsModerator { get; set; }

        public bool IsSubscriber { get; set; }

        public bool IsTurbo { get; set; }

        public string Login { get; set; }

        public string Message { get; set; }

        public string MsgId { get; set; }

        public string MsgParamRitualName { get; set; }

        public string RoomId { get; set; }

        public string SystemMsgParsed { get; set; }

        public string SystemMsg { get; set; }

        public string TmiSentTs { get; set; }

        public string UserId { get; set; }

        public UserType UserType { get; set; }

        public RitualNewChatter()
        {

        }

        // badges=subscriber/0;color=#0000FF;display-name=KittyJinxu;emotes=30259:0-6;id=1154b7c0-8923-464e-a66b-3ef55b1d4e50;
        // login=kittyjinxu;mod=0;msg-id=ritual;msg-param-ritual-name=new_chatter;room-id=35740817;subscriber=1;
        // system-msg=@KittyJinxu\sis\snew\shere.\sSay\shello!;tmi-sent-ts=1514387871555;turbo=0;user-id=187446639;
        // user-type= USERNOTICE #thorlar kittyjinxu > #thorlar: HeyGuys
        public RitualNewChatter(IrcMessage ircMessage)
        {
            Message = ircMessage.Message;
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
                    case Tags.MsgParamRitualName:
                        MsgParamRitualName = tagValue;
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
    }
}
