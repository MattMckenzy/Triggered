using Microsoft.EntityFrameworkCore;
using Triggered.Extensions;
using Triggered.Models;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Core.HttpCallHandlers;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace Triggered.Services
{
    public class TwitchServiceBase
    {
        #region Private Properties

        private readonly IDbContextFactory<TriggeredDbContext> _dbContextFactory;
        private readonly MessagingService _messagingService;
        private readonly EncryptionService _encryptionService;

        private Guid _stateId;

        private string _settingModifier = "";

        protected CancellationTokenSource _cancellationTokenSource = new();

        protected List<AuthScopes> Scopes { get; } = new();

        #endregion

        #region Public Properties

        public TwitchAPI TwitchAPI { get; private set; } = new();

        public async Task<bool> IsInitialized()
        {
                return !string.IsNullOrWhiteSpace(await TwitchAPI.Settings.GetClientIdAsync()) &&
                   !string.IsNullOrWhiteSpace(await TwitchAPI.Settings.GetClientSecretAsync());            
        }

        public async Task<bool> IsLoggedIn()
        {
            return !string.IsNullOrWhiteSpace(await TwitchAPI.Settings.GetAccessTokenAsync()) &&
                !string.IsNullOrWhiteSpace(await TwitchAPI.Settings.GetRefreshTokenAsync());
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(ChannelName) &&
                !string.IsNullOrWhiteSpace(UserName);
        }

        public event EventHandler<EventArgs>? ServiceStatusChanged;
        public bool? IsActive { get; set; } = false;

        public User? User { get; set; }
        public ChannelInformation? ChannelInformation { get; set; }

        public string ChannelName { get { return _dbContextFactory.CreateDbContext().Settings.GetSetting($"Twitch{_settingModifier}ChannelName"); } }
        public string UserName { get { return _dbContextFactory.CreateDbContext().Settings.GetSetting($"Twitch{_settingModifier}UserName"); } }

        #endregion

        #region Constructor

        public TwitchServiceBase(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, EncryptionService encryptionService)
        {
            _dbContextFactory = dbContextFactory;
            _messagingService = messagingService;
            _encryptionService = encryptionService;

            using TriggeredDbContext triggeredDbContext = _dbContextFactory.CreateDbContext();
        }

        #endregion

        #region Control Methods

        public virtual async Task<bool> Initialize(string settingModifier = "")
        {
            _settingModifier = settingModifier;

            using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();

            TwitchAPI = new(settings: new ApiSettings(
                async () => {
                    using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
                    return await _encryptionService.Decrypt($"Twitch{_settingModifier}ClientId", triggeredDbContext.Settings.GetSetting($"Twitch{_settingModifier}ClientId"));
                },
                async () => {
                    using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
                    return await _encryptionService.Decrypt($"Twitch{_settingModifier}ClientSecret", triggeredDbContext.Settings.GetSetting($"Twitch{_settingModifier}ClientSecret"));
                }, 
                async () => {
                    using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
                    return await _encryptionService.Decrypt($"Twitch{_settingModifier}AccessToken", triggeredDbContext.Settings.GetSetting($"Twitch{_settingModifier}AccessToken"));
                }, 
                async () => {
                    using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
                    return await _encryptionService.Decrypt($"Twitch{_settingModifier}RefreshToken", triggeredDbContext.Settings.GetSetting($"Twitch{_settingModifier}RefreshToken"));
                },
                true,
                true,
                Scopes
            ),
            http: new TwitchWebRequest(
                logger: null,
                async () => await GetValidToken(true),
                async (Exception exception) => await _messagingService.AddMessage(exception.Message, MessageCategory.Service, LogLevel.Error)
            ));

            return await CheckInitialized() && await CheckConfiguration();
        }

        protected Func<Task> ConnectFunction { get; set; } = null!;
        protected Func<Task> DisconnectFunction { get; set; } = null!;

        public async Task StartAsync()
        {
            if (!await CheckInitialized() || !await CheckLoggedIn() || !await CheckConfiguration())
                return;

            _cancellationTokenSource = new CancellationTokenSource();

            _ = Task.Run(async () =>
            {
                try
                {
                    await _messagingService.AddMessage($"Twitch{_settingModifier} service starting.", MessageCategory.Service, LogLevel.Debug);
                    IsActive = null;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    await ConnectFunction();

                    await _messagingService.AddMessage($"Twitch{_settingModifier} service started!", MessageCategory.Service);
                    IsActive = true;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());

                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(1000);
                    }
                }
                finally
                {
                    await DisconnectFunction();

                    IsActive = false;
                    ServiceStatusChanged?.Invoke(this, new EventArgs());
                    await _messagingService.AddMessage($"Twitch{_settingModifier} service stopped!", MessageCategory.Service);
                }

            }, _cancellationTokenSource.Token);
        }

        public Task StopAsync()
        {
            _cancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }

        #endregion

        #region Auth Methods

        public async Task<string?> GetAuthorizationCodeUrl(string redirectUri)
        {
            if (!await CheckInitialized())
                return null;

            _stateId = Guid.NewGuid();
            return await TwitchAPI.Auth.GetAuthorizationCodeUrl(redirectUri, Scopes, true, _stateId.ToString());
        }

        public async Task<bool> GetAccessToken(string code, string redirectUri, string? state)
        {
            if (!_stateId.ToString().Equals(state))
            {
                await _messagingService.AddMessage($"There was a problem verifying the identity of Twitch{_settingModifier}'s authentication provider! Please try logging in again.", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }

            if (!await CheckInitialized())
                return false;

            AuthCodeResponse authCodeResponse = await TwitchAPI.Auth.GetAccessTokenFromCodeAsync(code, redirectUri);

            if (authCodeResponse != null)
            {
                using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();

                triggeredDbContext.Settings.SetSetting($"Twitch{_settingModifier}AccessToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}AccessToken", authCodeResponse.AccessToken));
                triggeredDbContext.Settings.SetSetting($"Twitch{_settingModifier}RefreshToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}RefreshToken", authCodeResponse.RefreshToken));

                await GetChannelInformation();

                return true;
            }
            else
            {
                await _messagingService.AddMessage($"The Twitch{_settingModifier} access token retrieval failed, please try again.", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }
        }

        public async Task<bool> GetChannelInformation()
        { 
            if (!await CheckInitialized() || !await CheckLoggedIn() || !await CheckConfiguration())
                return false;

            User = (await TwitchAPI.Helix.Users.GetUsersAsync(logins: new List<string> { UserName! }))?
                    .Users.Where(user => user.DisplayName.Equals(UserName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            
            if (User == null)
            {
                await _messagingService.AddMessage($"The provided \"Twitch{_settingModifier}UserName\" was not found on Twitch!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }

            ChannelInformation = (await TwitchAPI.Helix.Channels.GetChannelInformationAsync(User.Id))?
                    .Data.Where(channel => channel.BroadcasterId.Equals(User.Id)).FirstOrDefault();

            return true;
        }

        protected async Task<string?> GetValidToken(bool forRefresh = true)
        {
            if (!await CheckInitialized() || !await CheckLoggedIn() || !await CheckConfiguration())
                return null;

            if (forRefresh)
                await _messagingService.AddMessage("Received unauthorized from Twitch, trying to refresh token.", MessageCategory.Authentication, LogLevel.Debug);

            ValidateAccessTokenResponse validateAccessTokenResponse = await TwitchAPI.Auth.ValidateAccessTokenAsync();
            string accessToken = await TwitchAPI.Auth.GetAccessToken();

            if (validateAccessTokenResponse != null)
            {
                return accessToken;
            }
            else
            {
                RefreshResponse refreshTokenResponse = await TwitchAPI.Auth.RefreshAuthTokenAsync();
                if (refreshTokenResponse != null)
                {
                    using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();

                    triggeredDbContext.Settings.SetSetting($"Twitch{_settingModifier}AccessToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}AccessToken", refreshTokenResponse.AccessToken));
                    triggeredDbContext.Settings.SetSetting($"Twitch{_settingModifier}RefreshToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}RefreshToken", refreshTokenResponse.RefreshToken));

                    return refreshTokenResponse.AccessToken;
                }
                else
                {
                    await _messagingService.AddMessage($"Could not refresh Twitch{_settingModifier} token!", MessageCategory.Authentication, LogLevel.Error);
                    await Logout();
                    return null;
                }
            }
        }

        public async Task Logout()
        {
            await StopAsync();

            await TwitchAPI.Auth.RevokeAccessTokenAsync();

            using TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();

            triggeredDbContext.Settings.SetSetting($"Twitch{_settingModifier}AccessToken", string.Empty);
            triggeredDbContext.Settings.SetSetting($"Twitch{_settingModifier}RefreshToken", string.Empty);
            await triggeredDbContext.SaveChangesAsync();

            User = null;
            ChannelInformation = null;
        }

        #endregion

        #region Private Helpers

        private async Task<bool> CheckInitialized()
        {
            if (!await IsInitialized())
            {
                await _messagingService.AddMessage($"\"Twitch{_settingModifier}ClientId\" and \"Twitch{_settingModifier}ClientSecret\" were not found in the settings!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }
            else 
                return true;
        }

        private async Task<bool> CheckLoggedIn()
        {
            if (!await IsLoggedIn())
            {
                await _messagingService.AddMessage($"Please login to your Twitch{_settingModifier} account before starting the service!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }
            else
                return true;
        }

        private async Task<bool> CheckConfiguration()
        {
            if (!IsConfigured())
            {
                await _messagingService.AddMessage($"\"Twitch{_settingModifier}ChannelName\" and \"Twitch{_settingModifier}UserName\" were not found in the settings!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }
            else
                return true;
        }

        #endregion
    }
}
