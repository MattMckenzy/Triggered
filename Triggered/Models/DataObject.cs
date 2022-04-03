using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    [Index(nameof(Depth))]
    public class DataObject
    {
        public DataObject(string key, int depth)
        {
            Key = key;
            Depth = depth;
        }

        [Required]
        [Key]
        public string Key { get; set; }

        [Required]
        public int Depth { get; set; }

        public string? ExpandoObjectJson {get;set;}
    }
}
