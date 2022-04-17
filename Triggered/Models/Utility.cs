using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    public class Utility
    {
        public Utility(
            string name = "",
            string code = "",
            bool isEnabled = true)
        {
            Name = name;
            Code = code;
            IsEnabled = isEnabled;
        }

        [Required]
        [Key]
        public int? Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public bool IsEnabled { get; set; }
    }
}
