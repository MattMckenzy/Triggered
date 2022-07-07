namespace Triggered.Models.Dashboard
{
    public class ButtonCommand
    {
        public ButtonCommand(string buttonName, string commandName)
        {
            ButtonName = buttonName;
            CommandName = commandName;
        }

        public int Order { get; set; } = 0;

        public string ButtonName { get; set; }
        public Button Button { get; set; } = null!;

        public string CommandName { get; set; }
        public Command Command { get; set; } = null!;
    }
}