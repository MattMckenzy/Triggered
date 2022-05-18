using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;

// TODO: finish controller.
namespace Triggered.Controllers
{
    /// <summary>
    /// Controller class offering endpoints to retrieve and save data.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {
        private DataService DataService { get; }
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }
        private MemoryCache MemoryCache { get; }
        private MessagingService MessagingService { get; }

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>        
        /// <param name="dataService">Injected <see cref="Services.DataService"/>.</param>
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="memoryCache">Injected <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        public DataController(
            DataService dataService,
            IDbContextFactory<TriggeredDbContext> dbContextFactory,
            MemoryCache memoryCache,
            MessagingService messagingService)
        {
            DataService = dataService;
            DbContextFactory = dbContextFactory;
            MemoryCache = memoryCache;
            MessagingService = messagingService;
        }

        ///// <summary>
        ///// Retrieves the value of a specific property inside a given data object through its key and path.
        ///// </summary>
        ///// <param name="key">The dot-syntax key used find the <see cref="ExpandoObject"/> that will be used to retrive its property (i.e. "Twitch.Users.MattMckenzy").</param>
        ///// <param name="path">The JSON path string that refers to the property to retrieve (i.e. "Title", see "https://www.newtonsoft.com/json/help/html/QueryJsonSelectTokenJsonPath.htm").</param>
        ///// <returns>The value of the property.</returns>
        //public async Task<string?> GetDataValue(string key, string path)
        //{
        //    try
        //    {
        //        return await DataService.GetValueWithPath(key, path);
        //    }
        //    catch (KeyNotFoundException exception)
        //    {
        //        await MessagingService.AddMessage(exception.Message, MessageCategory.Service, LogLevel.Error);
        //    }

        //    return null;
        //}

        ///// <summary>
        ///// Sets the value of a specific property inside a given data object through its key and path.
        ///// </summary>
        ///// <param name="key">The dot-syntax key used find the <see cref="ExpandoObject"/> that will be used to set its property (i.e. "Twitch.Users.MattMckenzy").</param>
        ///// <param name="path">The JSON path string that refers to the property to set (i.e. "Title", see "https://www.newtonsoft.com/json/help/html/QueryJsonSelectTokenJsonPath.htm").</param>
        ///// <param name="value">The value to set at the given property.</param>
        //public async Task SetDataValue(string key, string path, string value)
        //{
        //    try
        //    {
        //        await DataService.SetValueWithPath(key, path, value);
        //    }
        //    catch (KeyNotFoundException exception)
        //    {
        //        await MessagingService.AddMessage(exception.Message, MessageCategory.Service, LogLevel.Error);
        //    }
        //}

        ///// <summary>
        ///// Retrieves a DB <see cref="Setting"/> with the given <paramref name="name"/>. If not found, returns the default value or an empty string if no default value defined.
        ///// </summary>
        ///// <param name="key">The name of the setting to retrieve.</param>
        ///// <returns>The value of the setting, or a default or empty string if not found.</returns>
        //public async Task<string> GetSettingValue(string key)
        //{
        //    return (await DbContextFactory.CreateDbContextAsync()).Settings.GetSetting(key);
        //}

        ///// <summary>
        ///// Saves a DB <see cref="Setting"/> with the given <paramref name="name"/> and <paramref name="value"/>.
        ///// </summary>
        ///// <param name="key">The name of the setting to create or update.</param>
        ///// <param name="value">The value to set.</param>
        //public async Task SetSettingValue(string key, string value)
        //{
        //    (await DbContextFactory.CreateDbContextAsync()).Settings.SetSetting(key, value);
        //}

        ///// <summary>
        ///// Retrieves a <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/> item with the given <paramref name="key"/>. If not found, returns a null object.
        ///// </summary>
        ///// <param name="key">The key of the cache item to retrieve.</param>
        ///// <returns>The value of the setting, or a null object if not found.</returns>
        //public Task<object?> GetCacheValue(object key)
        //{
        //    if (MemoryCache.TryGetValue(key, out object result))
        //        return Task.FromResult((object?)result);
        //    else
        //        return Task.FromResult((object?)null);
        //}

        ///// <summary>
        ///// Sets a <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/> item found at the given <paramref name="key"/> with the given value.
        ///// </summary>
        ///// <param name="key">The key of the cache item to set.</param>
        ///// <param name="value">The value to set.</param>
        //public Task SetCacheValue(object key, object value)
        //{
        //    MemoryCache.Set(key, value);
        //    return Task.CompletedTask;
        //}
    }
}
