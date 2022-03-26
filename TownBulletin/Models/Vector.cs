using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TownBulletin.Models
{
    public class Vector
    {
        public Vector(string key, string value)
        {
            Key = key;
            Value = value;
        }

        [Required]
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }
    }
}
