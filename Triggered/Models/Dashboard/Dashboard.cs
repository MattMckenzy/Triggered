using System.ComponentModel.DataAnnotations;

namespace Triggered.Models.Dashboard
{
    /// <summary>
    /// Represents a dashboard with buttons created for quick module execution or changes.
    /// </summary>
    public class Dashboard
    {
        /// <summary>
        /// Default constructor with mandatory parameter.
        /// </summary>
        /// <param name="name">The unique name of the dashboard.</param>
        public Dashboard(string name)
        {
            Name = name;
        }

        /// <summary>
        /// The unique name of the dashboard.
        /// </summary>
        [Key]
        public string Name { get; set; }

        /// <summary>
        /// An optional description for the dashboard.
        /// </summary>
        public string? Description { get; set; }

        public ICollection<Button> Buttons { get; set; } = new List<Button>();
    }
}
