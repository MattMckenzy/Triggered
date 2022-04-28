namespace Triggered.Models
{
    /// <summary>
    /// An enumeration of possible <see cref="Message"/> categories.
    /// </summary>
    public enum MessageCategory
    {
        Undefined,
        Service,
        Authentication,
        Event,
        Module,
        Utility,
        Testing
    }
}