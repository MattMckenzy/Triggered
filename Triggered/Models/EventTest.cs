using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Triggered.Models
{
    public class EventTest
    {
        public EventTest(string name = "", string @event = "", string? jsonData = null)
        {
            Name = name;
            Event = @event;
            JsonData = jsonData;
        }

        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Event { get; set; }

        public string? JsonData { get; set; }
    }
}
