using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using Triggered.Models;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that contains methods to assist in generating Widget markup and regestering.
    /// </summary>
    public class WidgetService
    {
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; set; } = null!;

        private MemoryCache MemoryCache { get; }
        private MessagingService MessagingService { get; }
        private DataService DataService { get; }

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="dataService">Injected <see cref="Services.DataService"/>.</param>
        /// <param name="memoryCache">Injected <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        public WidgetService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MemoryCache memoryCache, MessagingService messagingService, DataService dataService)
        {
            DbContextFactory = dbContextFactory;
            MemoryCache = memoryCache;
            MessagingService = messagingService;
            DataService = dataService;
        }

        /// <summary>
        /// Replaces the HTML comment tokens in the given widget markup with their refeerenced values from the database or in-memory cache.
        /// </summary>
        /// <param name="Widget">The <see cref="Widget"/> containing the markup in which to replace tokens.</param>
        /// <remarks>
        /// The HTML comment tokens that are replaced with referenced values need to be in the following format: "<!--(.*?)-->" where "(.*?)" is replaced by the reference to either the key and JSON path (written as "key->path") of a "DataObject" starting with "data:", 
        /// the key of a "Setting" starting with "setting:" or the key of an object in <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/> starting with "cache:" (i.e., "<!--data:Adventure.Polls.Start->Title-->" or "<!--setting:TwitchUserName-->").
        /// </remarks>
        public async Task<string> ReplaceTokens(Widget widget)
        {
            TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            string markup = widget.Markup;
            foreach (Match match in Regex.Matches(markup, "<!--(.*?)-->"))
            {
                string token = match.Groups[1].Value;

                if (token.StartsWith("data:"))
                {
                    string[] selectorTokens = token[5..].Split("->", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                    if (selectorTokens.Length != 2)
                    {
                        await MessagingService.AddMessage($"The data token \"{token[5..]}\" in the widget \"{widget.Key}\" is not valid. Please make sure it is of the format \"data:**key**->**JSonPath**\".", MessageCategory.Widget, LogLevel.Error);
                        continue;
                    }

                    try
                    {
                        markup = markup.Replace(match.Value, await DataService.GetValueWithPath(selectorTokens[0], selectorTokens[1]));                        
                    }
                    catch(KeyNotFoundException exception)
                    {
                        await MessagingService.AddMessage(exception.Message, MessageCategory.Widget, LogLevel.Error);
                    }

                }
                else if (token.StartsWith("setting:"))
                {
                    string key = token[8..];
                    Setting? setting = triggeredDbContext.Settings.FirstOrDefault(setting => setting.Key.Equals(key));
                    if (setting == null)
                    {
                        await MessagingService.AddMessage($"Could not find the setting \"{token[8..]}\" for widget \"{widget.Key}\".", MessageCategory.Widget, LogLevel.Error);
                        continue;
                    }

                    markup = markup.Replace(match.Value, setting.Value);
                }
                else if (token.StartsWith("cache:"))
                {
                    if (!MemoryCache.TryGetValue(token[6..], out object cache))
                    {
                        await MessagingService.AddMessage($"Could not find the cache item \"{token[8..]}\" for widget \"{widget.Key}\".", MessageCategory.Widget, LogLevel.Error);
                        continue;
                    }

                    markup = markup.Replace(match.Value, cache.ToString());
                }
            }

           return markup;
        }
    }
}