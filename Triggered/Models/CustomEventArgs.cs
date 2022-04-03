namespace Triggered.Models
{
    public class CustomEventArgs
    {
        public object? Sender { get; set; }
        public string? Identifier { get; set; }
        public object? Data { get; set; }
    }
}
