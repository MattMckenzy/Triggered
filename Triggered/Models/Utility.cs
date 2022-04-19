using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    public class Utility
    {
        public Utility(
            string name = "",
            string code = "")
        {
            Name = name;
            Code = code;
        }

        [Required]
        [Key]
        public int? Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Code { get; set; }
    }
}
