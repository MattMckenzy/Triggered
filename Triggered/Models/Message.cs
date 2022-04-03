namespace Triggered.Models
{
    public class Message
    {
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

        public int Id { get; set; }

        public MessageCategory Category { get; set; }

        public string Text { get; set; }

        public LogLevel Severity { get; set; }

        public DateTime TimeStamp { get; set; }
    }
}
