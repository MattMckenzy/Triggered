using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    /// <summary>
    /// Defines a unit of code that is compiled and can be reused in <see cref="Module"/> code.
    /// </summary>
    public class Utility
    {
        /// <summary>
        /// Default constructor with optional parameters.
        /// </summary>
        /// <param name="name">The descriptive name of the <see cref="Utility"/>.</param>
        /// <param name="code">The code that will be compiled and available for reuse in <see cref="Module"/>s.</param>
        public Utility(
            string name = "",
            string code = "")
        {
            Name = name;
            Code = code;
        }

        /// <summary>
        /// The unique identifier of the <see cref="Utility"/>.
        /// </summary>
        [Required]
        [Key]
        public int? Id { get; set; }

        /// <summary>
        /// The descriptive name of the <see cref="Utility"/>.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The code that will be compiled and available for reuse in <see cref="Module"/>s.
        /// </summary>
        [Required]
        public string Code { get; set; }
    }
}
