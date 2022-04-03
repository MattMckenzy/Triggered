using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using TwitchLib.Api.Core.Enums;

namespace TwitchLib.Api.Core.Interfaces
{
    public interface IApiSettings
    {
        bool SkipDynamicScopeValidation { get; set; }
        bool SkipAutoServerTokenGeneration { get; set; }
        List<AuthScopes> Scopes { get; set; }

        Task<string> GetAccessTokenAsync();
        Task<string> GetRefreshTokenAsync();
        Task<string> GetClientSecretAsync();
        Task<string> GetClientIdAsync();
    }
}