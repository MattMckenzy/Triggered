using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Text.Json;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Controllers
{
    /// <summary>
    /// Controller class offering endpoints to execute modules.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ExecuteController : ControllerBase
    {
        private ModuleService ModuleService { get; }
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="moduleService">Dependenccy-injected singleton instance of <see cref="ModuleService"/></param>
        /// <param name="dbContextFactory">Dependenccy-injected singleton instance of <see cref="IDbContextFactory{TriggeredDbContext}"/></param>
        public ExecuteController(ModuleService moduleService, IDbContextFactory<TriggeredDbContext> dbContextFactory)
        {
            ModuleService = moduleService;
            DbContextFactory = dbContextFactory;
        }

        /// <summary>
        /// Executes modules by triggering the event passed in from the query value <paramref name="name"/>, with the given event <paramref name="arguments"/> passed in from the body as JSON.
        /// </summary>
        /// <param name="name">From query, name of the event to trigger.</param>
        /// <param name="arguments">From body, JSON event arguments that will be passed to executing modules.</param>
        /// <returns>Ok (200) action result if succesful, bad request (400) if not.</returns>
        [HttpPost("event")]
        public async Task<IActionResult> ExecuteEvent([FromQuery] string? name, [FromBody] JsonElement? arguments)
        {
            if (name == null)
                return BadRequest("Please supply a valid event name in the query string!");

            try
            {
                await ModuleService.ExecuteModulesAsync(name, arguments.HasValue ? arguments.Value.ToString() : string.Empty);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Executes modules by triggering the event based on the event test <paramref name="Id"/> given.
        /// </summary>
        /// <param name="name">From query, the ID of the event test to trigger.</param>
        /// <returns>Ok (200) action result if succesful, bad request (400) if not.</returns>
        [HttpPost("testEvent")]
        public async Task<IActionResult> ExecuteTestEvent([FromQuery] int? Id)
        {
            if (Id == null)
                return BadRequest("Please supply a valid event ID in the query string!");

            EventTest? eventTest = (await DbContextFactory.CreateDbContextAsync()).EventTests.FirstOrDefault(eventTest => eventTest.Id == Id);

            if (eventTest == null)
                return NotFound($"Could not find event test with ID {Id}");

            try
            {
                await ModuleService.ExecuteModulesAsync(eventTest.Event, eventTest.JsonData ?? string.Empty);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok();
        }

        /// <summary>
        /// Executes a module found by the query value <paramref name="id"/>, with the given event <paramref name="arguments"/> passed in from the body as JSON.
        /// </summary>
        /// <param name="id">From query, the id of the query to execute.</param>
        /// <param name="arguments">From body, JSON event arguments that will be passed to executing modules.</param>
        /// <returns>Ok (200) action result if succesful, bad request (400) if not.</returns>
        [HttpPost("module")]
        public async Task<IActionResult> ExecuteModule([FromQuery] int? id, [FromBody] JsonElement? arguments)
        {
            if (id == null)
                return BadRequest("Please supply a valid module ID in the query string! (I.e. \"?id=12\")");

            try
            {
                await ModuleService.ExecuteModuleAsync((int)id, arguments.HasValue ? arguments.Value.ToString() : string.Empty);
            }
            catch (ArgumentException exception)
            {
                return BadRequest(exception.Message);
            }

            return Ok();
        }
    }
}
