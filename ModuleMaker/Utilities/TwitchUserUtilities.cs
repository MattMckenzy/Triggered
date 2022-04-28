using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace ModuleMaker.Utilities
{
    public static class TwitchUserUtilities
    {
        /// <summary>
        /// Retrieves from cache or from Twitch a dynamic <see cref="ExpandoObject"/> containing information on a Twitch user.
        /// </summary>
        /// <param name="dataService">An instance of <see cref="DataService"/> that is used to cache the retrieved Twitch user.</param>
        /// <param name="twitchService">An instance of <see cref="TwitchService"/> that is used to retrieve a user.</param>
        /// <param name="messagingService">An instance of <see cref="MessagingService"/> that is used to warn of issues with the Twitch API communication.</param>
        /// <param name="userId">The ID of the user to retrieve.</param>
        /// <returns>An <see cref="ExpandoObject"/> that contains the following properties: Expires, Name, Id, Login, ProfileImageUrl, Type.</returns>
        public static async Task<ExpandoObject?> GetTwitchUser(DataService dataService, TwitchService twitchService, MessagingService messagingService, string userId)
        {
            ExpandoObject? user = await dataService.GetObject($"Twitch.Users.{userId}");
            if (user == null || ((dynamic)user).Expires < DateTime.Now)
            {
                GetUsersResponse usersResponse = await twitchService.TwitchAPI.Helix.Users.GetUsersAsync(new List<string> { userId });
                User? twitchUser = usersResponse.Users.FirstOrDefault();
                if (twitchUser != null)
                {
                    user = new ExpandoObject();
                    ((dynamic)user).Expires = DateTime.Now + TimeSpan.FromHours(1);
                    ((dynamic)user).Name = twitchUser.DisplayName;
                    ((dynamic)user).Id = twitchUser.Id;
                    ((dynamic)user).Login = twitchUser.Login;
                    ((dynamic)user).ProfileImageUrl = twitchUser.ProfileImageUrl;
                    ((dynamic)user).Type = twitchUser.Type;
                    await dataService.SetObject($"Twitch.Users.{userId}", user);
                }
                else
                {
                    await messagingService.AddMessage("Could not retrieve user from Twitch.", MessageCategory.Module, LogLevel.Error);
                    return null;
                }
            }

            return user;
        }
    }
}