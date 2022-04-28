using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    /// <summary>
    /// Defines an encryption vector.
    /// </summary>
    public class Vector
    {
        /// <summary>
        /// Default constructor with mandatory parameters.
        /// </summary>
        /// <param name="key">The unique key that references the <see cref="Vector"/>.</param>
        /// <param name="value">The value of the <see cref="Vector"/>.</param>
        public Vector(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// The unique key that references the <see cref="Vector"/>.
        /// </summary>
        [Required]
        [Key]
        public string Key { get; set; }

        /// <summary>
        /// The value of the <see cref="Vector"/>.
        /// </summary>
        [Required]
        public string Value { get; set; }
    }
}
