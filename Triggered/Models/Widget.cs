using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    /// <summary>
    /// Defines an HTML widget that is can be viewed in a browser or browser source.
    /// </summary>
    public class Widget
    {
        /// <summary>
        /// Default constructor with optional parameters.
        /// </summary>
        /// <param name="key">The uniquely identifying key of the <see cref="Widget"/>.</param>
        /// <param name="markup">The HTML markup that will be displayed when browsed to the current widget.</param>
        public Widget(
            string key = "",
            string markup = "")
        {
            Key = key;
            Markup = markup;
        }

        /// <summary>
        /// The uniquely identifying key of the <see cref="Widget"/>.
        /// </summary>
        [Required]
        [Key]
        public string Key { get; set; }

        /// <summary>
        /// The HTML markup that will be displayed when browsed to the current widget.
        /// </summary>
        [Required]
        public string Markup { get; set; }
    }
}
