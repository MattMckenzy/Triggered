using Newtonsoft.Json;

namespace TwitchLib.Api.V5.Models.Videos
{
    public class VideoUpload
    {
        #region Token
        [JsonProperty(PropertyName = "token")]
        public string Token { get; protected set; }
        #endregion
        #region Url
        [JsonProperty(PropertyName = "url")]
        public string Url { get; protected set; }
        #endregion
    }
}
