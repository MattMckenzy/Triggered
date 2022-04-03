using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Interfaces;

namespace TwitchLib.Api.Core
{
    public class ApiSettings : IApiSettings
    {
        public ApiSettings(Func<Task<string>> clientIdProducer = null,
                           Func<Task<string>> clientSecretProducer = null,
                           Func<Task<string>> accessTokenProducer = null,
                           Func<Task<string>> refreshTokenProducer = null,
                           bool skipDynamicScopeValidation = false,
                           bool skipAutoServerTokenGeneration = false,
                           List<AuthScopes> scopes = null)
        {
            ClientIdProducer = clientIdProducer;
            ClientSecretProducer = clientSecretProducer;
            AccessTokenProducer = accessTokenProducer;
            RefreshTokenProducer = refreshTokenProducer;
            SkipDynamicScopeValidation = skipDynamicScopeValidation;
            SkipAutoServerTokenGeneration = skipAutoServerTokenGeneration;
            Scopes = scopes;
        }

        private Func<Task<string>> ClientIdProducer;
        private Func<Task<string>> ClientSecretProducer;
        private Func<Task<string>> AccessTokenProducer;
        private Func<Task<string>> RefreshTokenProducer;

        public async Task<string> GetClientIdAsync()
        {
            if (ClientIdProducer == null)
                return null;
            else
                return await ClientIdProducer();
        }

        public async Task<string> GetClientSecretAsync()
        {
            if (ClientSecretProducer == null)
                return null;
            else
                return await ClientSecretProducer();
        }

        public async Task<string> GetAccessTokenAsync()
        {
            if (AccessTokenProducer == null)
                return null;
            else
                return await AccessTokenProducer();
        }

        public async Task<string> GetRefreshTokenAsync()
        {
            if (RefreshTokenProducer == null)
                return null;
            else
                return await RefreshTokenProducer();
        }


        public bool SkipDynamicScopeValidation { get; set; }
        public bool SkipAutoServerTokenGeneration { get; set; }
        public List<AuthScopes> Scopes { get; set; }
    }
}
