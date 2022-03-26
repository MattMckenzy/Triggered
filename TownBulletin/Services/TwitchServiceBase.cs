using Microsoft.EntityFrameworkCore;
using TownBulletin.Extensions;
using TownBulletin.Models;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.Helix.Models.Channels.GetChannelInformation;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace TownBulletin.Services
{
    public class TwitchServiceBase
    {
        #region Private Properties

        private readonly IDbContextFactory<TownBulletinDbContext> _dbContextFactory;
        private readonly MessagingService _messagingService;
        private readonly EncryptionService _encryptionService;

        private Guid _stateId;

        private string _settingModifier = "";

        protected CancellationTokenSource _cancellationTokenSource = new();

        protected List<AuthScopes> Scopes { get; } = new();

        #endregion

        #region Public Properties

        public TwitchAPI TwitchAPI { get; } = new();

        public bool IsInitialized
        {
            get
            {
                return !string.IsNullOrWhiteSpace(TwitchAPI.Settings.ClientId) &&
                    !string.IsNullOrWhiteSpace(TwitchAPI.Settings.ClientSecret);
            }
        }

        public bool IsLoggedIn
        {
            get
            {
                return !string.IsNullOrWhiteSpace(TwitchAPI.Settings.ClientId) &&
                    !string.IsNullOrWhiteSpace(TwitchAPI.Settings.RefreshToken);
            }
        }

        public bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(ChannelName) &&
                    !string.IsNullOrWhiteSpace(UserName);
            }
        }

        public event EventHandler<EventArgs>? ServiceStatusChanged;
        public bool IsActive { get; set; } = false;

        public User? User { get; set; }
        public ChannelInformation? ChannelInformation { get; set; }

        public string? ChannelName { get; set; }
        public string? UserName { get; set; }
              
        #endregion

        #region Constructor

        public TwitchServiceBase(IDbContextFactory<TownBulletinDbContext> dbContextFactory, MessagingService messagingService, EncryptionService encryptionService)
        {
            _dbContextFactory = dbContextFactory;
            _messagingService = messagingService;
            _encryptionService = encryptionService;
        }

        #endregion

        #region Control Methods

        public virtual async Task<bool> Initialize(string settingModifier = "")
        {
            _settingModifier = settingModifier;
            using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

            TwitchAPI.Settings.ClientId = await _encryptionService.Decrypt($"Twitch{_settingModifier}ClientId", townBulletinDbContext.Settings.GetSetting($"Twitch{_settingModifier}ClientId"));
            TwitchAPI.Settings.ClientSecret = await _encryptionService.Decrypt($"Twitch{_settingModifier}ClientSecret", townBulletinDbContext.Settings.GetSetting($"Twitch{_settingModifier}ClientSecret"));
            TwitchAPI.Settings.AccessToken = await _encryptionService.Decrypt($"Twitch{_settingModifier}AccessToken", townBulletinDbContext.Settings.GetSetting($"Twitch{_settingModifier}AccessToken"));
            TwitchAPI.Settings.RefreshToken = await _encryptionService.Decrypt($"Twitch{_settingModifier}RefreshToken", townBulletinDbContext.Settings.GetSetting($"Twitch{_settingModifier}RefreshToken"));
            TwitchAPI.Settings.SkipAutoServerTokenGeneration = true;
            TwitchAPI.Settings.Scopes = Scopes;

            ChannelName = townBulletinDbContext.Settings.GetSetting($"Twitch{_settingModifier}ChannelName");
            UserName = townBulletinDbContext.Settings.GetSetting($"Twitch{_settingModifier}UserName");

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
                await _messagingService.AddMessage($"Twitch{_settingModifier} service starting.", MessageCategory.Service, LogLevel.Debug);
                IsActive = true;
                ServiceStatusChanged?.Invoke(this, new EventArgs());

                try
                {
                    await ConnectFunction();

                    await _messagingService.AddMessage($"Twitch{_settingModifier} service started!", MessageCategory.Service);
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

        public async Task<TOut?> CallTwitchAPI<TOut>(Func<Task<TOut>> apiDelegate, bool refreshedToken = false)
        {
            try
            {
                return await apiDelegate();
            }
            catch (Exception exception) when (exception is UnauthorizedException)
            {
                if (refreshedToken == false)
                {
                    await _messagingService.AddMessage("Received unauthorized from Twitch, trying to refresh token.", MessageCategory.Authentication, LogLevel.Debug);
                    await GetValidToken();
                    return await CallTwitchAPI(apiDelegate, true);
                }
                else
                {
                    await _messagingService.AddMessage(exception.Message, MessageCategory.Authentication, LogLevel.Error);
                }
                    
            }
            catch (Exception exception) 
            when 
            (
                exception is BadRequestException ||
                exception is BadResourceException ||
                exception is NotPartneredException ||
                exception is TooManyRequestsException ||
                exception is BadGatewayException ||
                exception is GatewayTimeoutException ||
                exception is InternalServerErrorException ||
                exception is HttpRequestException
            )
            {
                await _messagingService.AddMessage(exception.Message, MessageCategory.Service, LogLevel.Error); 
            }

            return default;
        }

        #endregion

        #region Auth Methods

        public async Task<string?> GetAuthorizationCodeUrl(string redirectUri)
        {
            if (!await CheckInitialized())
                return null;

            _stateId = Guid.NewGuid();
            return TwitchAPI.Auth.GetAuthorizationCodeUrl(redirectUri, Scopes, true, _stateId.ToString());
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
                using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

                TwitchAPI.Settings.AccessToken = authCodeResponse.AccessToken;
                townBulletinDbContext.Settings.SetSetting($"Twitch{_settingModifier}AccessToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}AccessToken", authCodeResponse.AccessToken));
                TwitchAPI.Settings.RefreshToken = authCodeResponse.RefreshToken;
                townBulletinDbContext.Settings.SetSetting($"Twitch{_settingModifier}RefreshToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}RefreshToken", authCodeResponse.RefreshToken));

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

            User = (await CallTwitchAPI(
                async () => await TwitchAPI.Helix.Users.GetUsersAsync(logins: new List<string> { UserName! })))?
                    .Users.Where(user => user.DisplayName.Equals(UserName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            
            if (User == null)
            {
                await _messagingService.AddMessage($"The provided \"Twitch{_settingModifier}UserName\" was not found on Twitch!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }

            ChannelInformation = (await CallTwitchAPI(
                async () => await TwitchAPI.Helix.Channels.GetChannelInformationAsync(User.Id)))?
                    .Data.Where(channel => channel.BroadcasterId.Equals(User.Id)).FirstOrDefault();

            return true;
        }

        protected async Task<string?> GetValidToken()
        {
            if (!await CheckInitialized() || !await CheckLoggedIn() || !await CheckConfiguration())
                return null;

            ValidateAccessTokenResponse validateAccessTokenResponse = await TwitchAPI.Auth.ValidateAccessTokenAsync();
            string accessToken = TwitchAPI.Auth.GetAccessToken();

            if (validateAccessTokenResponse != null)
            {
                return accessToken;
            }
            else
            {
                RefreshResponse refreshTokenResponse = await TwitchAPI.Auth.RefreshAuthTokenAsync();
                if (refreshTokenResponse != null)
                {
                    using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

                    TwitchAPI.Settings.AccessToken = refreshTokenResponse.AccessToken;
                    townBulletinDbContext.Settings.SetSetting($"Twitch{_settingModifier}AccessToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}AccessToken", refreshTokenResponse.AccessToken));
                    TwitchAPI.Settings.RefreshToken = refreshTokenResponse.RefreshToken;
                    townBulletinDbContext.Settings.SetSetting($"Twitch{_settingModifier}RefreshToken", await _encryptionService.Encrypt($"Twitch{_settingModifier}RefreshToken", refreshTokenResponse.RefreshToken));
                }

                return TwitchAPI.Settings.AccessToken;
            }
        }

        public async Task Logout()
        {
            await StopAsync();

            await TwitchAPI.Auth.RevokeAccessTokenAsync();

            using TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();

            TwitchAPI.Settings.ClientId = null;
            TwitchAPI.Settings.ClientSecret = null;

            TwitchAPI.Settings.AccessToken = null;
            townBulletinDbContext.Settings.Remove(new Setting($"Twitch{_settingModifier}AccessToken", string.Empty));
            TwitchAPI.Settings.RefreshToken = null;
            townBulletinDbContext.Settings.Remove(new Setting($"Twitch{_settingModifier}RefreshToken", string.Empty));
            await townBulletinDbContext.SaveChangesAsync();

            UserName = null;
            ChannelName = null;

            User = null;
            ChannelInformation = null;
        }

        #endregion

        #region Private Helpers

        private async Task<bool> CheckInitialized()
        {
            if (!IsInitialized)
            {
                await _messagingService.AddMessage($"\"Twitch{_settingModifier}ClientId\" and \"Twitch{_settingModifier}ClientSecret\" were not found in the settings!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }
            else 
                return true;
        }

        private async Task<bool> CheckLoggedIn()
        {
            if (!IsLoggedIn)
            {
                await _messagingService.AddMessage($"Please login to your Twitch{_settingModifier} account before starting the service!", MessageCategory.Authentication, LogLevel.Error);
                return false;
            }
            else
                return true;
        }

        private async Task<bool> CheckConfiguration()
        {
            if (!IsConfigured)
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
