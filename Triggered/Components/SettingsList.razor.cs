using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;
using Triggered.Shared;

namespace Triggered.Components
{
    public partial class SettingsList
    {
        #region Services Injection

        [Inject]
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; set; } = null!;

        [Inject]
        private EncryptionService EncryptionService { get; set; } = null!;

        [Inject]
        private TwitchService TwitchService { get; set; } = null!;

        [Inject]
        private TwitchChatService TwitchChatService { get; set; } = null!;

        [Inject]
        private FileWatchingService FileWatchingService { get; set; } = null!;

        [CascadingParameter]
        public MainLayout MainLayout { get; set; } = null!;

        #endregion

        #region Private Variables

        private IEnumerable<Setting> Settings { get; set; } = Array.Empty<Setting>();
        private Dictionary<string, string>? SettingKeys { get; set; }
        private Dictionary<string, IEnumerable<string>>? CategoryKeys { get; set; }

        private Setting CurrentSetting { get; set; } = new();
        private bool CurrentSettingIsValid = false;
        private bool CurrentSettingIsDirty = false;
        private bool CurrentSettingKeyLocked = false;

        private ModalPrompt ModalPromptReference = null!;

        private readonly IEnumerable<string> EncryptedSettings = new string[]
        {
            "TwitchClientId",
            "TwitchClientSecret",
            "TwitchAccessToken",
            "TwitchRefreshToken",
            "TwitchChatClientId",
            "TwitchChatClientSecret",
            "TwitchChatAccessToken",
            "TwitchChatRefreshToken",
            "ObsPassword",
            "DiscordBotToken"
        };

        private readonly IEnumerable<string> TwitchLoginSettings = new string[]
        {
            "TwitchClientId",
            "TwitchClientSecret",
            "TwitchUserName",
            "TwitchChannelName",
            "TwitchAccessToken",
            "TwitchRefreshToken",
            "WebhookHost",
            "UseWebhookHostProxy"
        };

        private readonly IEnumerable<string> TwitchLoginResetSettings = new string[]
        {
            "TwitchAccessToken",
            "TwitchRefreshToken"
        };

        private readonly IEnumerable<string> TwitchChatLoginSettings = new string[]
        {
            "TwitchChatUseSecondAccount",
            "TwitchChatClientId",
            "TwitchChatClientSecret",
            "TwitchChatUserName",
            "TwitchChatChannelName",
            "TwitchChatAccessToken",
            "TwitchChatRefreshToken"
        };

        private readonly IEnumerable<string> TwitchChatLoginResetSettings = new string[]
        {
            "TwitchChatAccessToken",
            "TwitchChatRefreshToken"
        };

        private readonly IEnumerable<string> FileWatcherSettings = new string[]
        {
            "FileWatcherPaths"
        };

        private readonly IEnumerable<string> TriggeredSettings = new string[]
        {
            "Host",
            "WebhookHost",
            "UseWebhookHostProxy", 
            "Autostart",
            "ModuleTemplate",
            "ExternalModulesPath",
            "UtilityTemplate",
            "ExternalUtilitiesPath",
            "ExternalResourcesPath",
            "FileWatcherPaths",
            "MessagesLimit",
            "MessageLevels",
            "MessageNotificationsEnabled",
            "MessageNotificationVolume",
        };

        private readonly IEnumerable<string> TwitchSettings = new string[]
        {
            "TwitchClientId",
            "TwitchClientSecret",
            "TwitchAccessToken",
            "TwitchRefreshToken",
            "TwitchUserName",
            "TwitchChannelName",
            "TwitchExtensionSubscriptoins",
            "TwitchDropSubscriptoins"
        };

        private readonly IEnumerable<string> TwitchChatSettings = new string[]
        {
            "TwitchChatUseSecondAccount",
            "TwitchChatClientId",
            "TwitchChatClientSecret",
            "TwitchChatAccessToken",
            "TwitchChatRefreshToken",
            "TwitchChatUserName",
            "TwitchChatChannelName"
        };

        private readonly IEnumerable<string> ObsSettings = new string[]
        {
            "ObsAddress",
            "ObsPassword"
        };


        private readonly IEnumerable<string> DiscordSettings = new string[]
        {
            "DiscordBotToken"
        };

        #endregion

        #region Lifecycle Overrides

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await UpdatePageState();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        #endregion

        #region UI Events

        private async Task UpdateProperty(string propertyName, object value)
        {
            typeof(Setting).GetProperty(propertyName)!.SetValue(CurrentSetting, value);

            await UpdatePageState();
        }

        private async Task CreateSetting()
        {
            async void createAction()
            {
                CurrentSettingKeyLocked = false;

                await SetCurrentSetting(new Setting());
            }

            if (CurrentSettingIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to create a new setting and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = createAction
                });
            else
                createAction();
        }

        private async Task LoadSetting(string settingKey)
        {
            if (settingKey.Equals(CurrentSetting.Key, StringComparison.InvariantCultureIgnoreCase))
                return;

            Setting? setting = Settings.FirstOrDefault(setting => setting.Key.Equals(settingKey, StringComparison.InvariantCultureIgnoreCase));

            if (setting == null)
                return;            

            async void loadAction()
            {
                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentSetting((Setting)triggeredDbContext.Entry(setting).CurrentValues.Clone().ToObject());

                CurrentSettingKeyLocked = true;

                await InvokeAsync(StateHasChanged);
            }

            if (CurrentSettingIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to edit the setting \"{setting.Key}\" and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = loadAction
                });
            else
                loadAction();
        }

        private async Task SaveSetting()
        {
            if (!CurrentSettingIsValid || !CurrentSettingIsDirty)
                return;

            async Task saveSetting(IEnumerable<string>? resetSettings = null, TwitchServiceBase? twitchServiceBase = null, bool stopFileWatcherService = false)
            {
                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                if (EncryptedSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase))
                    CurrentSetting.Value = await EncryptionService.Encrypt(CurrentSetting.Key, CurrentSetting.Value);

                if (triggeredDbContext.Settings.Any(setting => setting.Key.Equals(CurrentSetting.Key)))
                {
                    triggeredDbContext.Settings.SetSetting(CurrentSetting.Key, CurrentSetting.Value);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Setting updated!",
                        Message = $"Successfully updated the setting \"{CurrentSetting.Key}\".",
                        CancelChoice = "Dismiss"
                    });
                }
                else
                {
                    triggeredDbContext.Settings.SetSetting(CurrentSetting.Key, CurrentSetting.Value);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Setting added!",
                        Message = $"Successfully added the setting \"{CurrentSetting.Key}\".",
                        CancelChoice = "Dismiss"
                    });
                }

                if (resetSettings != null)
                    foreach (string key in resetSettings)
                        triggeredDbContext.Settings.SetSetting(key, string.Empty);

                if (twitchServiceBase != null)
                    await twitchServiceBase.Logout();

                if ((resetSettings?.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase) ?? false) || CurrentSetting.Key.Equals("TwitchChatUseSecondAccount", StringComparison.InvariantCultureIgnoreCase))
                    await MainLayout.RefreshState();

                if (CurrentSetting.Key.Equals("TwitchChatUseSecondAccount", StringComparison.InvariantCultureIgnoreCase))
                    await TwitchChatService.Initialize();

                if (stopFileWatcherService)
                    await FileWatchingService.StopAsync();

                CurrentSettingKeyLocked = true;

                await UpdatePageState();

                return;
            }

            if (TwitchLoginSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase) && await TwitchService.IsLoggedIn())
            {
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Logging out!",
                    Message = $"Changing the value for the setting \"{CurrentSetting.Key}\" will log you out of the TwitchService! Proceed?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = async () => await saveSetting(TwitchLoginResetSettings, TwitchService)
                });
            }
            else if (TwitchChatLoginSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase) && await TwitchChatService.IsLoggedIn())
            {
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Logging out!",
                    Message = $"Changing the value for the setting \"{CurrentSetting.Key}\" will log you out of the TwitchChatService! Proceed?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = async () => await saveSetting(TwitchChatLoginResetSettings, TwitchChatService)
                });
            }
            else if (FileWatcherSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase))
            {
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Stopping File Watcher Service!",
                    Message = $"Changing the value for the setting \"{CurrentSetting.Key}\" will stop the file watcher service! Proceed?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = async () => await saveSetting(stopFileWatcherService: true)
                });
            }
            else
                await saveSetting();
        }

        private async Task DeleteSetting(string settingKey)
        {
            Setting? setting = Settings.FirstOrDefault(setting => setting.Key.Equals(settingKey, StringComparison.InvariantCultureIgnoreCase));

            if (setting == null)
                return;

            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Setting deletion!",
                Message = $"Are you sure you want to delete the setting \"{setting.Key}\"?",
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    if (CurrentSetting.Key.Equals(setting.Key, StringComparison.InvariantCultureIgnoreCase))
                        await CreateSetting();

                    using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                    triggeredDbContext.Remove(setting);
                    await triggeredDbContext.SaveChangesAsync();

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Setting removed!",
                        Message = $"Successfully removed the setting \"{setting.Key}\".",
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });

        }

        #endregion

        #region Helper Methods

        private async Task UpdatePageState()
        {
            TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            CurrentSettingIsValid =
                !string.IsNullOrWhiteSpace(CurrentSetting.Key);

            CurrentSettingIsDirty = (!triggeredDbContext.Settings.Any(setting => setting.Key.Equals(CurrentSetting.Key)) && !SettingComparer.MemberwiseComparer.Equals(CurrentSetting, new Setting())) ||
                (!string.IsNullOrWhiteSpace(CurrentSetting.Key) && !SettingComparer.MemberwiseComparer.Equals(CurrentSetting, triggeredDbContext.Settings.FirstOrDefault(setting => setting.Key.Equals(CurrentSetting.Key))));

            Settings = triggeredDbContext.Settings.ToList();
            SettingKeys = Settings.ToDictionary(module => module.Key, module => module.Key);
            CategoryKeys = new Dictionary<string, IEnumerable<string>>
            {
                { "Triggered", TriggeredSettings },
                { "Twitch Service", TwitchSettings },
                { "TwitchChat Service", TwitchChatSettings },
                { "OBS Service", ObsSettings },
                { "Discord Service", DiscordSettings },
                { "Custom", Settings.Select(setting => setting.Key)
                                .Except(TriggeredSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(TwitchSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(TwitchChatSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(ObsSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(DiscordSettings, StringComparer.InvariantCultureIgnoreCase)}
            };

            await InvokeAsync(StateHasChanged);
        }

        private async Task SetCurrentSetting(Setting setting)
        {
            CurrentSetting = setting;
            await UpdatePageState();
        }           

        #endregion
    }
}
