using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using TownBulletin.Extensions;
using TownBulletin.Models;
using TownBulletin.Services;
using TownBulletin.Shared;

namespace TownBulletin.Components
{
    public partial class SettingsList
    {
        #region Services Injection

        [Inject]
        private IDbContextFactory<TownBulletinDbContext> DbContextFactory { get; set; } = null!;

        [Inject]
        private EncryptionService EncryptionService { get; set; } = null!;

        [Inject]
        private TwitchService TwitchService { get; set; } = null!;

        [Inject]
        private TwitchBotService TwitchBotService { get; set; } = null!;

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
            "TwitchBotClientId",
            "TwitchBotClientSecret",
            "TwitchBotAccessToken",
            "TwitchBotRefreshToken",
            "ObsPassword"
        };

        private readonly IEnumerable<string> TwitchLoginSettings = new string[]
        {
            "TwitchClientId",
            "TwitchClientSecret",
            "TwitchUserName",
            "TwitchChannelName"
        };

        private readonly IEnumerable<string> TwitchLoginResetSettings = new string[]
        {
            "TwitchAccessToken",
            "TwitchRefreshToken"
        };

        private readonly IEnumerable<string> TwitchBotLoginSettings = new string[]
        {
            "TwitchBotClientId",
            "TwitchBotClientSecret",
            "TwitchBotUserName",
            "TwitchBotChannelName"
        };

        private readonly IEnumerable<string> TwitchBotLoginResetSettings = new string[]
        {
            "TwitchBotAccessToken",
            "TwitchBotRefreshToken"
        };

        private readonly IEnumerable<string> TownBulletinSettings = new string[]
        {
            "Host",
            "WebhookHost",
            "Autostart",
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

        private readonly IEnumerable<string> TwitchBotSettings = new string[]
        {
            "TwitchBotClientId",
            "TwitchBotClientSecret",
            "TwitchBotAccessToken",
            "TwitchBotRefreshToken",
            "TwitchBotUserName",
            "TwitchBotChannelName"
        };

        private readonly IEnumerable<string> ObsSettings = new string[]
        {
            "ObsAddress",
            "ObsPassword"
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
                using TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentSetting((Setting)townBulletinDbContext.Entry(setting).CurrentValues.Clone().ToObject());

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

            async Task saveSetting(IEnumerable<string>? resetSettings = null, TwitchServiceBase? twitchServiceBase = null)
            {
                using TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

                if (EncryptedSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase))
                    CurrentSetting.Value = await EncryptionService.Encrypt(CurrentSetting.Key, CurrentSetting.Value);

                if (townBulletinDbContext.Settings.Any(setting => setting.Key.Equals(CurrentSetting.Key)))
                {
                    townBulletinDbContext.Settings.SetSetting(CurrentSetting.Key, CurrentSetting.Value);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Setting updated!",
                        Message = $"Successfully updated the setting \"{CurrentSetting.Key}\".",
                        CancelChoice = "Dismiss"
                    });
                }
                else
                {
                    townBulletinDbContext.Settings.SetSetting(CurrentSetting.Key, CurrentSetting.Value);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Setting added!",
                        Message = $"Successfully added the setting \"{CurrentSetting.Key}\".",
                        CancelChoice = "Dismiss"
                    });
                }

                CurrentSettingKeyLocked = true;

                await UpdatePageState();

                return;
            }

            if (TwitchLoginSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase) && TwitchService.IsLoggedIn)
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
            else if (TwitchBotLoginSettings.Contains(CurrentSetting.Key, StringComparer.InvariantCultureIgnoreCase) && TwitchBotService.IsLoggedIn)
            {
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Logging out!",
                    Message = $"Changing the value for the setting \"{CurrentSetting.Key}\" will log you out of the TwitchBotService! Proceed?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = async () => await saveSetting(TwitchBotLoginResetSettings, TwitchBotService)
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

                    using TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

                    townBulletinDbContext.Remove(setting);
                    await townBulletinDbContext.SaveChangesAsync();

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
            TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

            CurrentSettingIsValid =
                !string.IsNullOrWhiteSpace(CurrentSetting.Key);

            CurrentSettingIsDirty = (!townBulletinDbContext.Settings.Any(setting => setting.Key.Equals(CurrentSetting.Key)) && !SettingComparer.MemberwiseComparer.Equals(CurrentSetting, new Setting())) ||
                (!string.IsNullOrWhiteSpace(CurrentSetting.Key) && !SettingComparer.MemberwiseComparer.Equals(CurrentSetting, townBulletinDbContext.Settings.FirstOrDefault(setting => setting.Key.Equals(CurrentSetting.Key))));

            Settings = townBulletinDbContext.Settings.ToList();
            SettingKeys = Settings.ToDictionary(module => module.Key, module => module.Key);
            CategoryKeys = new Dictionary<string, IEnumerable<string>>
            {
                { "TownBulletin", TownBulletinSettings },
                { "Twitch Service", TwitchSettings },
                { "TwitchBot Service", TwitchBotSettings },
                { "OBS Service", ObsSettings },
                { "Custom", Settings.Select(setting => setting.Key)
                                .Except(TownBulletinSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(TwitchSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(TwitchBotSettings, StringComparer.InvariantCultureIgnoreCase)
                                .Except(ObsSettings, StringComparer.InvariantCultureIgnoreCase) }
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
