using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Dynamic;
using Triggered.Models;

namespace Triggered.Services
{
    public class DataService
    {
        private readonly IDbContextFactory<TriggeredDbContext> _dbContextFactory;

        public DataService(IDbContextFactory<TriggeredDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ExpandoObject?> GetObject(string key)
        {
            TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
            string? expandoObjectJson = triggeredDbContext.DataObjects.FirstOrDefault(obj => obj.Key.Equals(key))?.ExpandoObjectJson;
            if (!string.IsNullOrWhiteSpace(expandoObjectJson))
                return JsonConvert.DeserializeObject<ExpandoObject>(expandoObjectJson);
            else
                return null;
        }

        public async Task<IEnumerable<ExpandoObject?>> GetChildren(string key)
        {
            TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
            IEnumerable<DataObject> dataObjects = triggeredDbContext.DataObjects
                .Where(dataObjects => dataObjects.Key.StartsWith(key) && dataObjects.Depth == key.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length + 1);

            return dataObjects.Where(dataObject => !string.IsNullOrWhiteSpace(dataObject.ExpandoObjectJson))
                .Select(dataObject => JsonConvert.DeserializeObject<ExpandoObject>(dataObject.ExpandoObjectJson!));
        }

        public async Task SetObject(string key, ExpandoObject expandoObject)
        {
            TriggeredDbContext triggeredDbContext = await _dbContextFactory.CreateDbContextAsync();
            DataObject dataObject = new(key, key.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length)
            {
                ExpandoObjectJson = JsonConvert.SerializeObject(expandoObject)
            };

            if (triggeredDbContext.DataObjects.Contains(dataObject))
                triggeredDbContext.Update(dataObject);
            else
                triggeredDbContext.Add(dataObject);

            await triggeredDbContext.SaveChangesAsync();
        }      
    }
}
    