using Triggered.Services;

namespace Triggered.Models
{
    /// <summary>
    /// Defines the event arguments for <see cref="ModuleService.OnCustomEvent"/>.
    /// </summary>
    public class CustomEventArgs
    {
        /// <summary>
        /// The sender object that invoked the event.
        /// </summary>
        public object? Sender { get; set; }

        /// <summary>
        /// A <see cref="string"/> that can be used to identify the intention of the event invokation.
        /// </summary>
        public string? Identifier { get; set; }

        /// <summary>
        /// An <see cref="object"/> that can old any useful data for the invoked event delegate.
        /// </summary>
        public object? Data { get; set; }
    }
}
