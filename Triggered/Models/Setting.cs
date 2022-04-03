using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Triggered.Models
{
    public class Setting
    {
        public Setting()
        {
            Key = string.Empty;
            Value = string.Empty;
        }

        public Setting(string key, string value)
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
