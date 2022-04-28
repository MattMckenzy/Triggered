using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Triggered.Models
{
    /// <summary>
    /// Respresents information for testing events stored in the database.
    /// </summary>
    public class EventTest
    {
        /// <summary>
        /// Default constructor with mandatory parameters.
        /// </summary>
        /// <param name="name">A descriptive name for an <see cref="EventTest"/>.</param>
        /// <param name="event">The unique key identifying the event that this <see cref="EventTest"/> will trigger.</param>
        /// <param name="jsonData">The event arguments that will be used during the test, serialized as JSON.</param>
        public EventTest(string name = "", string @event = "", string? jsonData = null)
        {
            Name = name;
            Event = @event;
            JsonData = jsonData;
        }

        /// <summary>
        /// The unique identifier for an <see cref="EventTest"/>. 
        /// </summary>
        [Required]
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? Id { get; set; }

        /// <summary>
        /// A descriptive name for 
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The unique key identifying the event that this <see cref="EventTest"/> will trigger.
        /// </summary>
        [Required]
        public string Event { get; set; }

        /// <summary>
        /// The event arguments that will be used during the test, serialized as JSON.
        /// </summary>
        public string? JsonData { get; set; }
    }
}
