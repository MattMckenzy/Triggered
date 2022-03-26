namespace TwitchLib.Api.Core.Enums
{
    public enum AuthScopes
    {
        // Api scopes
        AnalyticsReadExtensions,
        AnalyticsReadGames,
        BitsRead,
        UserEdit,
        ChannelEditCommercial,
        ChannelManageBroadcast,
        ChannelManageExtensions,
        ChannelManagePolls,
        ChannelManagePredictions,
        ChannelManageRedemptions,
        ChannelManageSchedule,
        ChannelManageVideos,
        ChannelReadEditors,
        ChannelReadGoals,
        ChannelReadHypeTrain,
        ChannelReadPolls,
        ChannelReadPredictions,
        ChannelReadRedemptions,
        ChannelReadStreamKey,
        ChannelReadSubscriptions,
        ClipsEdit,
        ModerationRead,
        ModeratorManageBannedUsers,
        ModeratorReadBlockedTerms,
        ModeratorManageBlockedTerms,
        ModeratorManageAutomod,
        ModeratorReadAutomodSettings,
        ModeratorManageAutomodSettings,
        ModeratorReadChatSettings,
        ModeratorManageChatSettings,
        UserManageBlockedUsers,
        UserReadBlockedUsers,
        UserReadBroadcast,
        UserReadEmail,
        UserReadFollows,
        UserReadSubscriptions,

        // Chat and PubSub scopes.
        ChannelModerate,
        ChatEdit,
        ChatRead,
        WhispersEdit,
        WhispersRead,

        // Other.
        None
    }
}
