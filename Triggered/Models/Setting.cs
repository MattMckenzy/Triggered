using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    /// <summary>
    /// Defines a configuration key and value.
    /// </summary>
    public class Setting
    {
        /// <summary>
        /// Default constuctor with empty parameters.
        /// </summary>
        public Setting()
        {
            Key = string.Empty;
            Value = string.Empty;
        }

        /// <summary>
        /// Default constructor with mandatory parameters.
        /// </summary>
        /// <param name="key">The unique key of the <see cref="Setting"/> that references the associated value.</param>
        /// <param name="value">The Value</param>
        public Setting(string key, string value)
        {
            Key = key;
            Value = value;
        }

        /// <summary>
        /// The unique key of the <see cref="Setting"/> that references the associated value.
        /// </summary>
        [Required]
        [Key]
        public string Key { get; set; }

        /// <summary>
        /// The value of the <see cref="Setting"/>.
        /// </summary>
        [Required]
        public string Value { get; set; }
    }
}
