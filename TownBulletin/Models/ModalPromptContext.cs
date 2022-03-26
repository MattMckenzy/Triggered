namespace TownBulletin.Models
{
    public class ModalPromptContext
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string CancelChoice { get; set; } = string.Empty;
        public string Choice { get; set; } = string.Empty;
        public string ChoiceColour { get; set; } = string.Empty;
        public Action? ChoiceAction { get; set; } = null;    
    }
}