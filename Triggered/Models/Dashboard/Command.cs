using System.ComponentModel.DataAnnotations;

namespace Triggered.Models.Dashboard
{
    public class Command
    {
        public Command(string name, CommandType commandType, string key, string value)
        {
            Name = name;
            CommandType = commandType;
            Key = key;
            Value = value;
        }

        [Key]
        public string Name { get; set; }

        public CommandType CommandType { get; set; } 

        public string Key { get; set; }

        public string Value { get; set; }

        public ICollection<Button> Buttons { get; set; } = new List<Button>();
        public List<ButtonCommand> ButtonCommands { get; set; } = new List<ButtonCommand>();
    }
}