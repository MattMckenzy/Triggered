using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Triggered.Models
{
    [Index(nameof(Event))]
    public class Module
    {
        public Module(
            string name = "",
            string @event = "ModuleService.OnCustomEvent",
            string code = "",
            string entryMethod = "",
            int executionOrder  = 0,
            bool stopEventExecution = false,
            bool isEnabled = true)
        {
            Name = name;
            Event = @event;
            Code = code;
            EntryMethod = entryMethod;
            ExecutionOrder = executionOrder;
            StopEventExecution = stopEventExecution;
            IsEnabled = isEnabled;
        }

        [Required]
        [Key]
        public int? Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Event { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string EntryMethod { get; set; }

        [Required]
        public int ExecutionOrder { get; set; }

        [Required]
        public bool StopEventExecution { get; set; }

        [Required]
        public bool IsEnabled { get; set; }
    }
}
