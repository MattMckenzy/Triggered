using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;

namespace Triggered.Models
{
    /// <summary>
    /// Respresents an object stored in the database as a serialized dynamic <see cref="ExpandoObject"/>.
    /// </summary>
    [Index(nameof(Depth))]
    public class DataObject
    {
        /// <summary>
        /// Default constructor with mandatory parameters.
        /// </summary>
        /// <param name="key">The unique key of the object.</param>
        /// <param name="depth">The depth of the key, as a dot-syntax structure (i.e. "Twitch.Users.MattMckenzy" has a depth of 3).</param>
        public DataObject(string key, int depth)
        {
            Key = key;
            Depth = depth;
        }

        /// <summary>
        /// The unique key of the object.
        /// </summary>
        [Required]
        [Key]
        public string Key { get; set; }

        /// <summary>
        /// The depth of the key, as a dot-syntax structure (i.e. "Twitch.Users.MattMckenzy" has a depth of 3).
        /// </summary>
        [Required]
        public int Depth { get; set; }

        /// <summary>
        /// The dynamic <see cref="ExpandoObject"/> serialized for database storage.
        /// </summary>
        public string? ExpandoObjectJson {get;set;}
    }
}
