using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Dynamic;
using TownBulletin.Models;

namespace TownBulletin.Services
{
    public class DataService
    {
        private readonly IDbContextFactory<TownBulletinDbContext> _dbContextFactory;

        public DataService(IDbContextFactory<TownBulletinDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }

        public async Task<ExpandoObject?> GetObject(string key)
        {
            TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();
            string? expandoObjectJson = townBulletinDbContext.DataObjects.FirstOrDefault(obj => obj.Key.Equals(key))?.ExpandoObjectJson;
            if (!string.IsNullOrWhiteSpace(expandoObjectJson))
                return JsonConvert.DeserializeObject<ExpandoObject>(expandoObjectJson);
            else
                return null;
        }

        public async Task<IEnumerable<ExpandoObject?>> GetChildren(string key)
        {
            TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();
            IEnumerable<DataObject> dataObjects = townBulletinDbContext.DataObjects
                .Where(dataObjects => dataObjects.Key.StartsWith(key) && dataObjects.Depth == key.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length + 1);

            return dataObjects.Where(dataObject => !string.IsNullOrWhiteSpace(dataObject.ExpandoObjectJson))
                .Select(dataObject => JsonConvert.DeserializeObject<ExpandoObject>(dataObject.ExpandoObjectJson!));
        }

        public async Task SetObject(string key, ExpandoObject expandoObject)
        {
            TownBulletinDbContext townBulletinDbContext = await _dbContextFactory.CreateDbContextAsync();
            DataObject dataObject = new(key, key.Split(".", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Length)
            {
                ExpandoObjectJson = JsonConvert.SerializeObject(expandoObject)
            };

            if (townBulletinDbContext.DataObjects.Contains(dataObject))
                townBulletinDbContext.Update(dataObject);
            else
                townBulletinDbContext.Add(dataObject);

            await townBulletinDbContext.SaveChangesAsync();
        }      
    }
}
    