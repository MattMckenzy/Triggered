using System.ComponentModel.DataAnnotations;

namespace Triggered.Models.Dashboard
{
    public class Button
    {
        public Button(string name)
        {
            Name = name;
        }

        [Key]
        public string Name { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }

        public int X { get; set; } = 0;
        public int Y { get; set; } = 0;
        public int Width { get; set; } = 10;
        public int Height { get; set; } = 10;

        public bool IsVisible { get; set; } = true;

        public ICollection<Command> Commands { get; set; } = new List<Command>();
        public List<ButtonCommand> ButtonCommands { get; set; } = new List<ButtonCommand>();
    }
}