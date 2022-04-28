namespace Triggered.Models
{
    /// <summary>
    /// Defines a message that is shown on the Triggered home page.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Default constructor with mandatory and optional parameters.
        /// </summary>
        /// <param name="text">The message's text.</param>
        /// <param name="id">The unique identifier of the message.</param>
        /// <param name="messageCategory">A message's category, a descriptive <see cref="enum"/>.</param>
        /// <param name="severity">Defines the message severity level, used for filtering messages.</param>
        /// <param name="timeStamp">The timestamp of the message.</param>
        public Message(
            string text,
            int id,
            MessageCategory messageCategory = MessageCategory.Undefined,
            LogLevel severity = LogLevel.Information,
            DateTime timeStamp = default)
        {
            Text = text;
            Id = id;
            Category = messageCategory;
            Severity = severity;
            TimeStamp = timeStamp == default ? DateTime.Now : timeStamp;
        }

        /// <summary>
        /// The unique identifier of the message.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// A message's category, a descriptive <see cref="enum"/>.
        /// </summary>
        public MessageCategory Category { get; set; }

        /// <summary>
        /// The message's text.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Defines the message severity level, used for filtering messages.
        /// </summary>
        public LogLevel Severity { get; set; }

        /// <summary>
        /// The timestamp of the message.
        /// </summary>
        public DateTime TimeStamp { get; set; }
    }
}
