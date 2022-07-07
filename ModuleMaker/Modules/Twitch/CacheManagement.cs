using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Triggered.Models;

namespace ModuleMaker.Modules.Custom
{
    public class CacheManagement
    {
        public static Task<bool> ManageCache(CustomEventArgs eventArgs, MemoryCache memoryCache) 
        {
            if (eventArgs.Identifier?.Equals("ManageCache", StringComparison.InvariantCultureIgnoreCase) ?? false)
            {
                JObject dataObject = JObject.Parse(eventArgs.Data?.ToString() ?? String.Empty);
                if (dataObject.TryGetValue("Type", StringComparison.InvariantCultureIgnoreCase, out JToken? typeToken) && typeToken != null)
                {
                    if (new string[]{ "Add", "Update", "AddOrUpdate", "AddUpdate", "UpdateOrAdd", "UpdateAdd" }
                            .Any(str => str.Equals(typeToken.Value<string>(), StringComparison.InvariantCultureIgnoreCase)) &&
                        dataObject.TryGetValue("Key", StringComparison.InvariantCultureIgnoreCase, out JToken? addKeyToken) && addKeyToken != null &&
                        dataObject.TryGetValue("Value", StringComparison.InvariantCultureIgnoreCase, out JToken? addValueToken) && addValueToken != null)
                    {
                        memoryCache.Set(addKeyToken.Value<string>(), addValueToken.Value<string>());
                    }
                    else if (new string[] { "Remove", "Delete", "Clear" }
                            .Any(str => str.Equals(typeToken.Value<string>(), StringComparison.InvariantCultureIgnoreCase)) &&
                        dataObject.TryGetValue("Key", StringComparison.InvariantCultureIgnoreCase, out JToken? removeKeyToken) && removeKeyToken != null)
                    {
                        memoryCache.Remove(removeKeyToken.Value<string>());
                    }
                }

                return Task.FromResult(false);
            }
            else
            {
                return Task.FromResult(true);
            }
        } 
    } 
}