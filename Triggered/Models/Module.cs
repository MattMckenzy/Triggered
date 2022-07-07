using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Triggered.Models
{
    /// <summary>
    /// Defines a unit meant to react to an event with given code.
    /// </summary>
    [Index(nameof(Event))]
    public class Module
    {
        /// <summary>
        /// Default constructor with mandatory and default parameters.
        /// </summary>
        /// <param name="name">The descriptive name of the <see cref="Module"/>.</param>
        /// <param name="event">The unique key referencing the event that will trigger this <see cref="Module"/>'s execution.</param>
        /// <param name="code">The code that will be compiled and executed.</param>
        /// <param name="entryMethod">The code's entry method that will be directly invoked when the event is triggered.</param>
        /// <param name="executionOrder">The placement of this <see cref="Module"/> in relation to all other <see cref="Module"/>s for the triggered event. Lower will be executed first.</param>
        /// <param name="stopEventExecution">Whether or not the triggered event will stop executing <see cref="Module"/>s after this one is complete. True will stop subsequent <see cref="Module"/>s in the order from being executed.</param>
        /// <param name="isEnabled">True to enable this <see cref="Module"/> for execution, false to disable it.</param>
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

        /// <summary>
        /// The unique identifier of the <see cref="Module"/>.
        /// </summary>
        [Required]
        [Key]
        public int? Id { get; set; }

        /// <summary>
        /// The descriptive name of the <see cref="Module"/>.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// The unique key referencing the event that will trigger this <see cref="Module"/>'s execution.
        /// </summary>
        [Required]
        public string Event { get; set; }

        /// <summary>
        /// The code that will be compiled and executed.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// The code's entry method that will be directly invoked when the event is triggered.
        /// </summary>
        [Required]
        public string EntryMethod { get; set; }

        /// <summary>
        /// The placement of this <see cref="Module"/> in relation to all other <see cref="Module"/>s for the triggered event. Lower will be executed first.
        /// </summary>
        [Required]
        public int ExecutionOrder { get; set; }

        /// <summary>
        /// Whether or not the triggered event will stop executing <see cref="Module"/>s after this one is complete. True will stop subsequent <see cref="Module"/>s in the order from being executed.
        /// </summary>
        [Required]
        public bool StopEventExecution { get; set; }

        /// <summary>
        /// True to enable this <see cref="Module"/> for execution, false to disable it.
        /// </summary>
        [Required]
        public bool IsEnabled { get; set; }
    }
}
